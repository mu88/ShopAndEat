// Content Script: Bridge between ShoppingAgent WASM app and Chrome Extension background worker.
// Runs on the ShopAndEat page (raspi or localhost).
// Listens for postMessage from WASM, forwards to background worker, returns results.

const i18n = chrome.i18n.getMessage.bind(chrome.i18n);

console.log(i18n('logBridgeLoaded'));

// Notify WASM app that extension is connected
window.postMessage({ type: 'SHOP_EXTENSION_CONNECTED' }, window.location.origin);

// Listen for tool requests from the WASM app
window.addEventListener('message', (event) => {
  if (event.source !== window) return;
  if (event.origin !== window.location.origin) return;

  if (event.data?.type === 'SHOP_TOOL_REQUEST') {
    const request = event.data.request;

    if (!request || typeof request !== 'object' || !request.tool || !request.args) {
      console.error('Invalid SHOP_TOOL_REQUEST format');
      return;
    }

    console.log(i18n('logBridgeToolRequest'), request.tool, request.args);

    // Forward to background worker
    const BRIDGE_TIMEOUT_MS = 30000;
    let timeoutId = setTimeout(() => {
      window.postMessage({
        type: 'SHOP_TOOL_RESULT',
        result: { success: false, error: 'Extension bridge timeout - service worker may have been terminated', id: request.id }
      }, window.location.origin);
    }, BRIDGE_TIMEOUT_MS);

    chrome.runtime.sendMessage(
      { type: 'TOOL_REQUEST', request },
      (response) => {
        clearTimeout(timeoutId);
        if (chrome.runtime.lastError) {
          console.error(i18n('logBridgeError'), chrome.runtime.lastError.message);
          window.postMessage({
            type: 'SHOP_TOOL_RESULT',
            result: { success: false, error: chrome.runtime.lastError.message }
          }, window.location.origin);
          return;
        }

        console.log(i18n('logBridgeToolResult'), response);
        window.postMessage({ type: 'SHOP_TOOL_RESULT', result: response }, window.location.origin);
      }
    );
  }

  if (event.data?.type === 'SHOPPING_AGENT_READY') {
    console.log(i18n('logBridgeWasmReady'));
    window.postMessage({ type: 'SHOP_EXTENSION_CONNECTED' }, window.location.origin);
  }
});

// Detect when extension disconnects
chrome.runtime.onMessage.addListener((message) => {
  if (message.type === 'EXTENSION_DISCONNECTED') {
    window.postMessage({ type: 'SHOP_EXTENSION_DISCONNECTED' }, window.location.origin);
  }
});
