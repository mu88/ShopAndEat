using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text.Json;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.JSInterop;
using ShoppingAgent.Diagnostics;
using ShoppingAgent.Logging;
using ShoppingAgent.Models;
using ShoppingAgent.Options;
using ShoppingAgent.Resources;

namespace ShoppingAgent.Services.Concrete;

/// <summary>
/// JS Interop bridge to communicate with the Chrome extension via postMessage.
/// The extension's content script on the ShopAndEat page listens for messages,
/// forwards them to the background worker, which executes tools on the target shop's tab.
/// </summary>
public sealed class ExtensionBridge : IExtensionBridge
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };
    private readonly IJSRuntime _jsRuntime;
    private readonly IStringLocalizer<Messages> _localizer;
    private readonly ILogger<ExtensionBridge> _logger;
    private readonly ExtensionOptions _extensionOptions;
    private readonly ConcurrentDictionary<string, TaskCompletionSource<ToolResult>> _pendingCalls = new(StringComparer.Ordinal);
    private DotNetObjectReference<ExtensionBridge> _dotNetRef;
    private bool _extensionConnected;

    public ExtensionBridge(IJSRuntime jsRuntime, IStringLocalizer<Messages> localizer, ILogger<ExtensionBridge> logger, IOptions<ExtensionOptions> extensionOptions)
    {
        _jsRuntime = jsRuntime;
        _localizer = localizer;
        _logger = logger;
        _extensionOptions = extensionOptions.Value;
    }

#pragma warning disable MA0046
    public event Action OnConnectionChanged;
#pragma warning restore MA0046

    public bool IsExtensionConnected => _extensionConnected;

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        _dotNetRef?.Dispose();
        _dotNetRef = DotNetObjectReference.Create(this);
        await _jsRuntime.InvokeVoidAsync("extensionBridge.initialize", cancellationToken, _dotNetRef);
        ServiceLogMessages.ExtensionBridgeInitialized(_logger);
    }

    /// <summary>
    /// Sends a tool call to the extension and waits for the result.
    /// </summary>
    public async Task<ToolResult> ExecuteToolAsync(string toolName, IDictionary<string, object> arguments, string shopKey, CancellationToken cancellationToken = default)
    {
        if (!_extensionConnected)
        {
            ServiceLogMessages.ExtensionNotConnected(_logger, toolName);
            return new ToolResult { Success = false, Error = _localizer["ExtensionNotConnectedError"].Value };
        }

        var callId = Guid.NewGuid().ToString();
        var tcs = new TaskCompletionSource<ToolResult>();
        _pendingCalls[callId] = tcs;

        var request = new { tool = toolName, args = arguments, shop = shopKey, id = callId };
        await _jsRuntime.InvokeVoidAsync("extensionBridge.sendToolCall", cancellationToken, JsonSerializer.Serialize(request));

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(TimeSpan.FromSeconds(_extensionOptions.ToolCallTimeoutSeconds));

        using var activity = ShoppingAgentDiagnostics.ActivitySource.StartActivity("ShoppingAgent.ExtensionBridge.Invoke");
        activity?.SetTag("extension.tool", toolName);
        activity?.SetTag("extension.shop", shopKey);
        activity?.SetTag("extension.call_id", callId);
        try
        {
            var result = await tcs.Task.WaitAsync(cts.Token);
            activity?.SetTag("extension.success", result.Success);
            if (!result.Success)
            {
                activity?.SetStatus(ActivityStatusCode.Error, result.Error);
            }

            return result;
        }
        catch (OperationCanceledException)
        {
            ServiceLogMessages.ToolCallTimedOut(_logger, toolName, callId);
            activity?.SetStatus(ActivityStatusCode.Error, "timeout");
            return new ToolResult { Success = false, Error = _localizer["ToolCallTimeout"].Value };
        }
        finally
        {
            _pendingCalls.TryRemove(callId, out _);
        }
    }

    [JSInvokable]
    public void OnToolResult(string resultJson)
    {
        try
        {
            var result = JsonSerializer.Deserialize<ToolResult>(resultJson, JsonOptions);
            result ??= new ToolResult { Success = false, Error = _localizer["EmptyExtensionResponse"].Value };

            if (!string.IsNullOrEmpty(result.Id) && _pendingCalls.TryGetValue(result.Id, out var tcs))
            {
                tcs.TrySetResult(result);
            }
        }
        catch (Exception ex)
        {
            ServiceLogMessages.ToolResultParseFailed(_logger, ex.Message);
            var error = new ToolResult { Success = false, Error = string.Format(_localizer["ParseError"], ex.Message) };
            foreach (var entry in _pendingCalls)
            {
                entry.Value.TrySetResult(error);
            }
        }
    }

    [JSInvokable]
    public void OnExtensionConnected()
    {
        _extensionConnected = true;
        ServiceLogMessages.ExtensionConnected(_logger);
        OnConnectionChanged?.Invoke();
    }

    [JSInvokable]
    public void OnExtensionDisconnected()
    {
        _extensionConnected = false;
        ServiceLogMessages.ExtensionDisconnected(_logger);
        OnConnectionChanged?.Invoke();
    }

    public async ValueTask DisposeAsync()
    {
        if (_dotNetRef != null)
        {
            try
            {
                await _jsRuntime.InvokeVoidAsync("extensionBridge.dispose");
            }
            catch (JSDisconnectedException)
            {
                // Circuit already disconnected — JS interop is no longer available
            }

            _dotNetRef.Dispose();
        }
    }
}
