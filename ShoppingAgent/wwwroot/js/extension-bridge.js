// Extension Bridge - JS Interop for communication between Blazor WASM and Chrome Extension
// Communication flow: WASM <-> postMessage <-> Extension Content Script <-> Background Worker <-> Shop Tab

window.extensionBridge = {
    _dotNetRef: null,
    _initialized: false,

    initialize: function (dotNetRef) {
        this._dotNetRef = dotNetRef;
        this._initialized = true;

        // Listen for messages from the extension's content script
        window.addEventListener('message', (event) => {
            if (event.source !== window) return;

            if (event.data?.type === 'SHOP_TOOL_RESULT') {
                dotNetRef.invokeMethodAsync('OnToolResult', JSON.stringify(event.data.result));
            }

            if (event.data?.type === 'SHOP_EXTENSION_CONNECTED') {
                dotNetRef.invokeMethodAsync('OnExtensionConnected');
            }

            if (event.data?.type === 'SHOP_EXTENSION_DISCONNECTED') {
                dotNetRef.invokeMethodAsync('OnExtensionDisconnected');
            }
        });

        // Announce that the WASM app is ready
        window.postMessage({ type: 'SHOPPING_AGENT_READY' }, '*');
        console.log('[ShoppingAgent] Extension bridge initialized');
    },

    sendToolCall: function (requestJson) {
        window.postMessage({
            type: 'SHOP_TOOL_REQUEST',
            request: JSON.parse(requestJson)
        }, '*');
    },

    dispose: function () {
        this._dotNetRef = null;
        this._initialized = false;
    }
};
