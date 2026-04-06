using System.Reflection;
using Microsoft.Extensions.Localization;
using ShoppingAgent.Resources;

namespace ShoppingAgent.Services.Concrete;

/// <summary>
/// Builds the LLM system prompt including learned preferences and unit configuration.
/// The template is loaded once from an embedded resource (SystemPrompt.txt).
/// </summary>
public class SystemPromptBuilder : ISystemPromptBuilder
{
    private static readonly Lazy<string> PromptTemplate = new(() =>
    {
        var assembly = typeof(SystemPromptBuilder).Assembly;
        using var stream = assembly.GetManifestResourceStream("ShoppingAgent.Resources.SystemPrompt.txt")
            ?? throw new InvalidOperationException("Embedded resource 'ShoppingAgent.Resources.SystemPrompt.txt' not found.");
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    });

    private readonly IPreferencesService _preferencesService;
    private readonly ISessionService _sessionService;
    private readonly IStringLocalizer<Messages> _localizer;

    public SystemPromptBuilder(
        IPreferencesService preferencesService,
        ISessionService sessionService,
        IStringLocalizer<Messages> localizer)
    {
        _preferencesService = preferencesService;
        _sessionService = sessionService;
        _localizer = localizer;
    }

    public async Task<string> BuildSystemPromptAsync(string shopName, string shopUrl, string shopKey, CancellationToken ct = default)
    {
        var preferencesTask = _preferencesService.GetAllPreferencesAsync(shopKey, ct);
        var unitsTask = _sessionService.GetUnitsAsync(ct);
        await Task.WhenAll(preferencesTask, unitsTask);

        var preferences = await preferencesTask;
        var units = await unitsTask;

        var prefText = preferences.Count > 0
            ? $"{Environment.NewLine}{Environment.NewLine}{_localizer["LearnedPreferences"]}{Environment.NewLine}" + string.Join(Environment.NewLine, preferences.Select(pref => $"- [{pref.Scope}] {pref.Key}: `{pref.Value.Replace('\n', ' ').Replace("\r", string.Empty, StringComparison.Ordinal).Replace('`', '\'')}`"))
            : string.Empty;

        var unitList = string.Join(", ", units.Select(unit => $"\"{unit}\""));

        return PromptTemplate.Value
            .Replace("{shopName}", shopName, StringComparison.Ordinal)
            .Replace("{shopUrl}", shopUrl, StringComparison.Ordinal)
            .Replace("{unitList}", unitList, StringComparison.Ordinal)
            .Replace("{prefText}", prefText, StringComparison.Ordinal);
    }
}
