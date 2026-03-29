// Content Script: Coop.ch DOM operations.
// Executed by the background worker via chrome.scripting.executeScript.
// This file provides functions that are injected into coop.ch pages.

const i18n = chrome.i18n.getMessage.bind(chrome.i18n);
const LOG_PREFIX = '[ShopAndEat:coop]';

// Multi-fallback selectors for Coop's CSS classes
const SELECTORS = {
  productTile: [
    '[class*="productTile"]',
    '[class*="ProductTile"]',
    '[class*="product-tile"]',
    'a[href*="/p/"]',
    '[data-testid*="product"]'
  ],
  productName: [
    '[class*="productTile-details__name"]',
    '[class*="productTile__name"]',
    '[class*="ProductName"]',
    '[class*="product-name"]',
    '[class*="productTile"] [class*="name"]',
    'h2', 'h3', '.name'
  ],
  productPrice: [
    '[class*="productTile__priceCurrent"]',
    '[class*="productTile__price"]',
    '[class*="ProductPrice"]',
    '[class*="price"]',
    '[class*="Price"]'
  ],
  productImage: [
    '[class*="productTile"] img',
    '[class*="ProductTile"] img',
    'img[src*="coop"]',
    'img[loading]'
  ],
  addToCartButton: [
    'button[data-dynamic-initialize="addToBasket"]',
    'button[class*="addToBasket__button"]',
    'button[class*="addToBasket"]',
    'button[class*="addToCart"]',
    'button[class*="AddToCart"]',
    'button[class*="add-to-cart"]',
    'button[aria-label*="Warenkorb"]',
    'button[aria-label*="hinzufügen"]'
  ],
  detailName: [
    'h1[class*="productBasicInfo"]',
    'h1[class*="productDetail"]',
    'h1[class*="ProductDetail"]',
    'h1[class*="product-detail"]',
    '.product-detail h1',
    'h1'
  ],
  detailPrice: [
    '[itemprop="price"]',
    '[data-testauto*="price"]',
    '[class*="productBasicInfo__price-current"]',
    '[class*="productBasicInfo__priceCurrent"]',
    '[class*="productDetail__price"]',
    '[class*="Price"]'
  ],
  detailBrand: [
    '[class*="productBasicInfo__brand"]',
    '[class*="productBasicInfo"] [class*="brand"]',
    '[class*="brand"]',
    '[class*="Brand"]',
    'a[href*="/brands/"]'
  ],
  detailUnitSize: [
    '[class*="productBasicInfo__quantity"]',
    '[data-testauto="productWeight"]',
    '[data-product-amount]',
    '[data-product-capacity]',
    '[class*="unitSize"]',
    '[class*="unit-size"]',
    '[class*="grammage"]',
    '[class*="Grammage"]',
    '[class*="basePrice"]'
  ],
  detailDescription: [
    '[class*="description"]',
    '[class*="Description"]',
    '[class*="product-info"]'
  ],
  searchInput: [
    'input[type="search"]',
    'input[name="query"]',
    'input[class*="search"]',
    'input[class*="Search"]',
    '#search'
  ]
};

function findFirst(selectorList, context = document, label = '') {
  for (const selector of selectorList) {
    const el = context.querySelector(selector);
    if (el) {
      console.log(`${LOG_PREFIX} findFirst(${label}): matched '${selector}'`);
      return el;
    }
  }
  console.log(`${LOG_PREFIX} findFirst(${label}): no match`);
  return null;
}

function findAll(selectorList, context = document, label = '') {
  for (const selector of selectorList) {
    const els = context.querySelectorAll(selector);
    if (els.length > 0) {
      console.log(`${LOG_PREFIX} findAll(${label}): matched '${selector}', count=${els.length}`);
      return Array.from(els);
    }
  }
  console.log(`${LOG_PREFIX} findAll(${label}): no match`);
  return [];
}

// Listen for tool execution requests from background worker
chrome.runtime.onMessage.addListener((message, sender, sendResponse) => {
  if (message.type === 'EXECUTE_TOOL') {
    handleTool(message.tool, message.args)
      .then(result => sendResponse(result))
      .catch(err => sendResponse({ success: false, error: err.message }));
    return true; // async response
  }
});

async function handleTool(toolName, args) {
  console.log(`${LOG_PREFIX} handleTool: ${toolName}`, args);
  let result;
  switch (toolName) {
    case 'search':
      result = await executeSearch(args.term);
      break;
    case 'getProductDetails':
      result = await getProductDetails();
      break;
    case 'addToCart':
      result = await addToCart(args.productUrl, args.quantity || 1, args.csrfToken);
      break;
    case 'removeFromCart':
      result = await removeFromCart(args.productName, args.csrfToken);
      break;
    case 'getCartContents':
      result = getCartContents();
      break;
    default:
      result = { success: false, error: i18n('unknownTool', toolName) };
  }
  console.log(`${LOG_PREFIX} handleTool result for ${toolName}:`, result);
  return result;
}

async function executeSearch(term) {
  console.log(`${LOG_PREFIX} executeSearch: "${term}" on ${window.location.href}`);

  // Scrape search results from current page
  const tiles = findAll(SELECTORS.productTile, document, 'productTile');

  if (tiles.length === 0) {
    console.log(`${LOG_PREFIX} executeSearch: no product tiles found`);
    return { success: true, data: JSON.stringify([]) };
  }

  const products = tiles.slice(0, 20).map((tile, idx) => {
    const nameEl = findFirst(SELECTORS.productName, tile, `productName[${idx}]`);
    const priceEl = findFirst(SELECTORS.productPrice, tile, `productPrice[${idx}]`);
    const imgEl = findFirst(SELECTORS.productImage, tile, `productImage[${idx}]`);
    const link = tile.tagName === 'A' ? tile : tile.querySelector('a[href*="/p/"]');

    return {
      name: nameEl?.textContent?.trim() || i18n('unknownProduct'),
      price: priceEl?.textContent?.trim() || '',
      url: link?.href || '',
      imageUrl: imgEl?.src || '',
      isAvailable: true
    };
  });

  console.log(`${LOG_PREFIX} executeSearch: found ${products.length} products`);
  return { success: true, data: JSON.stringify(products) };
}

async function getProductDetails() {
  console.log(`${LOG_PREFIX} getProductDetails on ${window.location.href}`);

  const nameEl = findFirst(SELECTORS.detailName, document, 'detailName');
  const priceEl = findFirst(SELECTORS.detailPrice, document, 'detailPrice');
  const brandEl = findFirst(SELECTORS.detailBrand, document, 'detailBrand');
  const unitSizeEl = findFirst(SELECTORS.detailUnitSize, document, 'detailUnitSize');
  const descEl = findFirst(SELECTORS.detailDescription, document, 'detailDescription');

  // Extract weight from structured data attributes (most reliable)
  let unitSize = '';
  const weightEl = document.querySelector('[data-testauto="productWeight"], [data-product-amount]');
  if (weightEl) {
    const amount = weightEl.textContent?.trim() || weightEl.getAttribute('data-product-amount') || '';
    const unitEl = weightEl.parentElement?.querySelector('meta[itemprop="unitCode"]');
    const unitCode = unitEl?.getAttribute('content') || '';
    const unitMap = { GRM: 'g', KGM: 'kg', MLT: 'ml', LTR: 'l', CMT: 'cm', MTR: 'm' };
    unitSize = `${amount}${unitMap[unitCode] || unitCode}`;
    console.log(`${LOG_PREFIX} getProductDetails: weight from structured data: ${unitSize}`);
  }
  if (!unitSize && unitSizeEl) {
    unitSize = unitSizeEl.textContent?.trim() || '';
  }

  // Extract price: prefer itemprop="price" content attribute, then text
  let price = '';
  const priceMetaEl = document.querySelector('[itemprop="price"]');
  if (priceMetaEl) {
    price = priceMetaEl.getAttribute('content') || priceMetaEl.textContent?.trim() || '';
  }
  if (!price && priceEl) {
    // Clean: take only the first number-like part (e.g. "3.95" from "3.95 CHF 3.95 Preis pro...")
    const rawPrice = priceEl.textContent?.trim() || '';
    const priceMatch = rawPrice.match(/(\d+\.\d{2})/);
    price = priceMatch ? priceMatch[1] : rawPrice.replace(/\s+/g, ' ').substring(0, 30);
  }

  // Availability: check if add-to-cart button exists (more reliable than text search)
  const addBtn = findFirst(SELECTORS.addToCartButton, document, 'availabilityCheck');
  const isAvailable = !!addBtn && !addBtn.disabled;

  const details = {
    name: nameEl?.textContent?.trim() || '',
    price: price,
    url: window.location.href,
    brand: brandEl?.textContent?.trim() || '',
    unitSize: unitSize,
    description: descEl?.textContent?.trim()?.substring(0, 500) || '',
    isAvailable: isAvailable
  };

  console.log(`${LOG_PREFIX} getProductDetails result:`, details);
  return { success: true, data: JSON.stringify(details) };
}

// --- CSRF Token & Cart API ---

function getCSRFToken() {
  // 1. Meta tags (standard hybris patterns)
  const meta = document.querySelector('meta[name="_csrf"], meta[name="csrf-token"], meta[name="CSRFToken"]');
  if (meta?.content) return meta.content;

  // 2. Hidden input named CSRFToken
  const input = document.querySelector('input[name="CSRFToken"]');
  if (input?.value) return input.value;

  // 3. Parse inline <script> tags for CSRFToken assignment
  for (const script of document.querySelectorAll('script:not([src])')) {
    const text = script.textContent;
    if (text) {
      const match = text.match(/CSRFToken\s*[=:]\s*["']([0-9a-f-]{36})["']/i);
      if (match) return match[1];
    }
  }

  // 4. Any hidden input containing csrf in name
  for (const el of document.querySelectorAll('input[type="hidden"]')) {
    if (el.name?.toLowerCase().includes('csrf')) return el.value;
  }

  console.log(`${LOG_PREFIX} getCSRFToken: not found via any strategy`);
  return null;
}

function extractProductCode(url) {
  const match = url?.match(/\/p\/(\d+)/);
  return match ? match[1] : null;
}

async function addToCart(productUrl, quantity, passedCsrfToken) {
  const productCode = extractProductCode(productUrl);
  if (!productCode) {
    console.log(`${LOG_PREFIX} addToCart: could not extract product code from "${productUrl}"`);
    return { success: false, error: i18n('errorProductIdExtraction', productUrl) };
  }

  const csrfToken = passedCsrfToken || getCSRFToken();
  if (!csrfToken) {
    return { success: false, error: i18n('errorCsrfTokenNotFound') };
  }

  const payload = `products%5B0%5D.productCode=${productCode}&products%5B0%5D.quantity=${quantity}&CSRFToken=${encodeURIComponent(csrfToken)}`;
  const headers = {
    'Content-Type': 'application/x-www-form-urlencoded; charset=UTF-8',
    'X-Requested-With': 'XMLHttpRequest'
  };

  console.log(`${LOG_PREFIX} addToCart API: productCode=${productCode}, quantity=${quantity}`);

  const controller = new AbortController();
  const fetchTimeout = setTimeout(() => controller.abort(), 15000);
  try {
    const response = await fetch('/de/basket/detail/add', {
      method: 'POST', headers, body: payload, signal: controller.signal
    });
    clearTimeout(fetchTimeout);

    const data = await response.json();
    console.log(`${LOG_PREFIX} addToCart API response:`, JSON.stringify(data).substring(0, 200));

    if (data.success && data.items?.length > 0) {
      const item = data.items[0];

      // Finalize cart entry (required for correct prices and checkout)
      if (data.additionalAjaxCall) {
        const ctrl2 = new AbortController();
        const fetchTimeout2 = setTimeout(() => ctrl2.abort(), 15000);
        try {
          await fetch(data.additionalAjaxCall, {
            method: 'POST', headers, body: `${payload}&async=true`, signal: ctrl2.signal
          });
          clearTimeout(fetchTimeout2);
          console.log(`${LOG_PREFIX} addToCart: cartupdate completed`);
        } catch (e) {
          clearTimeout(fetchTimeout2);
          console.log(`${LOG_PREFIX} addToCart: cartupdate failed (non-critical):`, e.message);
        }
      }

      const result = {
        added: item.json?.quantitySelector?.quantity ?? quantity,
        requested: quantity,
        verified: true,
        productCode,
        cartEntryUid: item.cartEntryUid,
        price: item.price,
        priceWeight: item.priceWeight
      };

      if (item.promotionText) {
        result.promoAvailable = true;
        result.promoText = item.promotionText;
      }

      return { success: true, data: JSON.stringify(result) };
    }

    // success:true but items:[] means product exists but is not available for online order
    if (data.success && (!data.items || data.items.length === 0)) {
      return { success: false, error: i18n('errorProductNotAvailable', productCode) };
    }

    return { success: false, error: i18n('errorApiFailed', JSON.stringify(data).substring(0, 200)) };
  } catch (err) {
    clearTimeout(fetchTimeout);
    console.log(`${LOG_PREFIX} addToCart API error:`, err.message);
    if (err.name === 'AbortError') {
      return { success: false, error: i18n('errorFetchTimeout') };
    }
    return { success: false, error: i18n('errorNetworkError', err.message) };
  }
}

async function removeFromCart(productName, passedCsrfToken) {
  console.log(`${LOG_PREFIX} removeFromCart: looking for "${productName}"`);

  const cartItems = document.querySelectorAll('[data-testauto="basket-product"], .basket-item[data-productid]');
  if (cartItems.length === 0) {
    return { success: false, error: i18n('cartEmptyOrWrongPage') };
  }

  // Find the item by matching the product name (case-insensitive partial match)
  const searchLower = productName.toLowerCase();
  let targetItem = null;
  let matchedName = '';

  for (const item of cartItems) {
    const nameEl = item.querySelector('[class*="basket-item__description"] a, [class*="basket-item"] a[href*="/p/"]');
    const name = nameEl?.textContent?.trim() || '';
    // Skip items with empty/short names to avoid false matches
    if (name.length < 3) continue;
    if (name.toLowerCase().includes(searchLower) || searchLower.includes(name.toLowerCase())) {
      targetItem = item;
      matchedName = name;
      break;
    }
  }

  // If no match by name, try matching by product URL path
  if (!targetItem) {
    for (const item of cartItems) {
      const linkEl = item.querySelector('a[href*="/p/"]');
      const href = linkEl?.getAttribute('href') || '';
      const name = linkEl?.textContent?.trim() || '';
      if (href && searchLower.split(' ').some(word => word.length > 3 && href.toLowerCase().includes(word))) {
        targetItem = item;
        matchedName = name || href;
        break;
      }
    }
  }

  if (!targetItem) {
    const allNames = Array.from(cartItems).map(item => {
      const el = item.querySelector('[class*="basket-item__description"] a, [class*="basket-item"] a[href*="/p/"]');
      return el?.textContent?.trim() || '(unbekannt)';
    });
    console.log(`${LOG_PREFIX} removeFromCart: no match for "${productName}". Items found:`, allNames);
    return { success: false, error: i18n('errorProductNotInCart', [productName, allNames.join(', ')]) };
  }

  const cartEntryUid = targetItem.getAttribute('data-cartentryuid');
  if (!cartEntryUid) {
    return { success: false, error: i18n('errorCartEntryUidNotFound', matchedName) };
  }

  const csrfToken = passedCsrfToken || getCSRFToken();
  if (!csrfToken) {
    return { success: false, error: i18n('errorCsrfTokenNotFound') };
  }

  console.log(`${LOG_PREFIX} removeFromCart API: cartEntryUid=${cartEntryUid} for "${matchedName}"`);

  const controller = new AbortController();
  const fetchTimeout = setTimeout(() => controller.abort(), 15000);
  try {
    const response = await fetch('/de/basket/basket/delete/cartentry/', {
      method: 'POST',
      headers: {
        'Content-Type': 'application/x-www-form-urlencoded; charset=UTF-8',
        'X-Requested-With': 'XMLHttpRequest'
      },
      body: `products%5B0%5D.cartEntryUid=${cartEntryUid}&CSRFToken=${encodeURIComponent(csrfToken)}`,
      signal: controller.signal
    });
    clearTimeout(fetchTimeout);

    const data = await response.json();
    console.log(`${LOG_PREFIX} removeFromCart API response:`, JSON.stringify(data).substring(0, 200));

    return { success: true, data: JSON.stringify({ removed: matchedName, cartEntryUid }) };
  } catch (err) {
    clearTimeout(fetchTimeout);
    console.log(`${LOG_PREFIX} removeFromCart API error:`, err.message);
    if (err.name === 'AbortError') {
      return { success: false, error: i18n('errorFetchTimeout') };
    }
    return { success: false, error: i18n('errorNetworkError', err.message) };
  }
}

function getCartContents() {
  const cartItems = document.querySelectorAll('[data-testauto="basket-product"], .basket-item[data-productid]');

  if (cartItems.length === 0) {
    return { success: true, data: JSON.stringify({ items: [], message: i18n('cartEmptyOrWrongPage') }) };
  }

  const items = Array.from(cartItems).map(item => {
    const nameEl = item.querySelector('[class*="basket-item__description"] a, [class*="basket-item"] a[href*="/p/"]');
    const priceEl = item.querySelector('[data-testauto="minibasketprod-price"], [class*="basket-item__price"]');
    const qtyInput = item.querySelector('.basket-item__step input, input[type="number"]');
    const productId = item.getAttribute('data-productid') || '';
    const cartEntryUid = item.getAttribute('data-cartentryuid') || '';
    const productUrl = nameEl?.href || '';

    return {
      name: nameEl?.textContent?.trim() || '',
      productId,
      cartEntryUid,
      productUrl,
      price: priceEl?.textContent?.trim() || '',
      quantity: qtyInput?.value || '1'
    };
  });

  return { success: true, data: JSON.stringify({ items }) };
}

console.log(i18n('logCoopLoaded'), window.location.href);
