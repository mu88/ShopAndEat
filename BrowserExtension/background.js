// Background Worker: Manages shop tabs and routes tool requests.
// Receives tool requests from content-bridge.js (on ShopAndEat page),
// navigates/executes on the target shop's tab, returns results.

const i18n = chrome.i18n.getMessage.bind(chrome.i18n);

const DEFAULT_BRIDGE_HOSTS = [
  'http://localhost:8080/shopAndEat/shopping*',
  'http://localhost:5176/shopAndEat/shopping*',
];

async function registerBridgeContentScript() {
  const { bridgeHosts } = await chrome.storage.sync.get({ bridgeHosts: DEFAULT_BRIDGE_HOSTS });
  try {
    await chrome.scripting.unregisterContentScripts({ ids: ['bridge'] });
  } catch {
    // Not yet registered — that's fine
  }
  await chrome.scripting.registerContentScripts([{
    id: 'bridge',
    matches: bridgeHosts,
    js: ['content-bridge.js'],
    runAt: 'document_idle',
  }]);
  console.log('[ShopAndEat:bg] Bridge content script registered for:', bridgeHosts);
}

chrome.runtime.onInstalled.addListener(registerBridgeContentScript);
chrome.runtime.onStartup.addListener(registerBridgeContentScript);

chrome.storage.onChanged.addListener((changes, area) => {
  if (area === 'sync' && changes.bridgeHosts) {
    registerBridgeContentScript();
  }
});

// Shop registry — add new shops here
const shops = {
  coop: {
    key: 'coop',
    baseUrl: 'https://www.coop.ch',
    cartUrl: 'https://www.coop.ch/de/cart',
    searchUrl: (term) => `https://www.coop.ch/de/search/?text=${encodeURIComponent(term)}`,
    urlPattern: 'coop.ch',
    tabId: null,
    contentScript: 'content-coop.js',
  },
};

function getShop(shopKey) {
  return shops[shopKey] || shops.coop;
}

// Listen for tool requests from the bridge content script
chrome.runtime.onMessage.addListener((message, sender, sendResponse) => {
  if (message.type === 'TOOL_REQUEST') {
    handleToolRequest(message.request)
      .then(result => sendResponse(result))
      .catch(err => sendResponse({ success: false, error: err.message }));
    return true; // async response
  }
});

async function handleToolRequest(request) {
  const { tool, args, shop: shopKey, id } = request;
  const shop = getShop(shopKey || 'coop');
  console.log(i18n('logBackgroundToolRequest'), tool, args, `[shop: ${shopKey}]`);

  let result;
  switch (tool) {
    case 'search':
      result = await handleSearch(shop, args.term);
      break;
    case 'getProductDetails':
      result = await handleGetProductDetails(shop, args.url);
      break;
    case 'addToCart':
      result = await handleAddToCart(shop, args.url, args.quantity);
      break;
    case 'removeFromCart':
      result = await handleRemoveFromCart(shop, args.productName, args.cartEntryUid);
      break;
    case 'getCartContents':
      result = await handleGetCartContents(shop);
      break;
    case 'navigateToCart':
      result = await handleNavigateToCart(shop);
      break;
    default:
      result = { success: false, error: i18n('unknownTool', tool) };
  }

  // Propagate the call ID so WASM can match the result to the pending request
  if (id) result.id = id;
  return result;
}

let _ensureTabPromise = null;

async function ensureShopTab(shop) {
  if (_ensureTabPromise) return _ensureTabPromise;
  _ensureTabPromise = _ensureShopTabImpl(shop);
  try {
    return await _ensureTabPromise;
  } finally {
    _ensureTabPromise = null;
  }
}

async function _ensureShopTabImpl(shop) {
  // Restore tabId from session storage
  const stored = await chrome.storage.session.get(`${shop.key}_tabId`);
  if (stored[`${shop.key}_tabId`]) {
    shop.tabId = stored[`${shop.key}_tabId`];
  }

  // Check if existing tab is still valid
  if (shop.tabId) {
    try {
      const tab = await chrome.tabs.get(shop.tabId);
      if (tab && tab.url?.includes(shop.urlPattern)) {
        return shop.tabId;
      }
    } catch {
      shop.tabId = null;
    }
  }

  // Find existing shop tab
  const tabs = await chrome.tabs.query({ url: `${shop.baseUrl}/*` });
  if (tabs.length > 0) {
    shop.tabId = tabs[0].id;
    await chrome.storage.session.set({ [`${shop.key}_tabId`]: shop.tabId });
    return shop.tabId;
  }

  // Create new tab
  const tab = await chrome.tabs.create({ url: `${shop.baseUrl}/de`, active: false });
  shop.tabId = tab.id;
  await chrome.storage.session.set({ [`${shop.key}_tabId`]: tab.id });
  await waitForTabLoad(shop.tabId);
  return shop.tabId;
}

function waitForTabLoad(tabId) {
  return new Promise((resolve, reject) => {
    const timeout = setTimeout(() => reject(new Error(i18n('tabLoadTimeout'))), 15000);

    function listener(updatedTabId, changeInfo) {
      if (updatedTabId === tabId && changeInfo.status === 'complete') {
        chrome.tabs.onUpdated.removeListener(listener);
        clearTimeout(timeout);
        // Extra delay for JS hydration
        setTimeout(resolve, 1500);
      }
    }

    chrome.tabs.onUpdated.addListener(listener);
  });
}

async function navigateAndWait(tabId, url) {
  await chrome.tabs.update(tabId, { url });
  await waitForTabLoad(tabId);
}

async function executeOnShopTab(shop, tool, args) {
  const tabId = await ensureShopTab(shop);

  try {
    const results = await chrome.tabs.sendMessage(tabId, {
      type: 'EXECUTE_TOOL',
      tool,
      args
    });
    return results;
  } catch (err) {
    // Content script may not be loaded yet (e.g. after navigation in SPA)
    console.log('[ShopAndEat:bg] sendMessage failed, injecting content script:', err.message);
    await chrome.scripting.executeScript({
      target: { tabId },
      files: [shop.contentScript]
    });
    await new Promise(r => setTimeout(r, 500));

    // Retry
    return await chrome.tabs.sendMessage(tabId, {
      type: 'EXECUTE_TOOL',
      tool,
      args
    });
  }
}

async function handleSearch(shop, term) {
  const tabId = await ensureShopTab(shop);
  const searchUrl = shop.searchUrl(term);

  await navigateAndWait(tabId, searchUrl);

  // Wait for search results to render
  await new Promise(r => setTimeout(r, 3000));

  let result = await executeOnShopTab(shop, 'search', { term });

  // Retry once if no results found (slow page render)
  if (result.success && result.data) {
    try {
      const parsed = JSON.parse(result.data);
      if (Array.isArray(parsed) && parsed.length === 0) {
        console.log('[ShopAndEat:bg] Search returned empty, retrying after extra wait...');
        await new Promise(r => setTimeout(r, 3000));
        result = await executeOnShopTab(shop, 'search', { term });
      }
    } catch {}
  }

  return result;
}

async function handleGetProductDetails(shop, url) {
  const tabId = await ensureShopTab(shop);

  if (url && url.startsWith('http')) {
    await navigateAndWait(tabId, url);
    await new Promise(r => setTimeout(r, 1000));
  }

  return await executeOnShopTab(shop, 'getProductDetails', {});
}

async function getCSRFTokenFromPage(tabId) {
  try {
    const results = await chrome.scripting.executeScript({
      target: { tabId },
      world: 'MAIN',
      func: () => {
        try {
          return (window.ACC && window.ACC.config && window.ACC.config.CSRFToken) || null;
        } catch { return null; }
      }
    });
    return results?.[0]?.result || null;
  } catch (err) {
    console.log('[ShopAndEat:bg] getCSRFToken via MAIN world failed:', err.message);
    return null;
  }
}

async function handleAddToCart(shop, url, quantity) {
  const tabId = await ensureShopTab(shop);
  const csrfToken = await getCSRFTokenFromPage(tabId);
  return await executeOnShopTab(shop, 'addToCart', { productUrl: url, quantity: quantity || 1, csrfToken });
}

async function handleGetCartContents(shop) {
  const tabId = await ensureShopTab(shop);
  await navigateAndWait(tabId, shop.cartUrl);
  await new Promise(r => setTimeout(r, 2000));

  return await executeOnShopTab(shop, 'getCartContents', {});
}

async function handleNavigateToCart(shop) {
  const tabId = await ensureShopTab(shop);
  await navigateAndWait(tabId, shop.cartUrl);
  return { success: true, data: 'Warenkorb geöffnet' };
}

async function handleRemoveFromCart(shop, productName, cartEntryUid) {
  const tabId = await ensureShopTab(shop);

  // Ensure we're on the cart page
  const tab = await chrome.tabs.get(tabId);
  if (!tab.url?.includes('/cart')) {
    await navigateAndWait(tabId, shop.cartUrl);
    await new Promise(r => setTimeout(r, 2000));
  }

  const csrfToken = await getCSRFTokenFromPage(tabId);
  return await executeOnShopTab(shop, 'removeFromCart', { productName, cartEntryUid, csrfToken });
}

// Clean up when any shop tab is closed
chrome.tabs.onRemoved.addListener((tabId) => {
  for (const shop of Object.values(shops)) {
    if (tabId === shop.tabId) {
      shop.tabId = null;
    }
  }
});

console.log(i18n('logBackgroundLoaded'));
