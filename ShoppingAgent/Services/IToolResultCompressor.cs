namespace ShoppingAgent.Services;

/// <summary>
/// Compresses raw tool results before they are stored in the LLM conversation history.
/// This prevents the context window from growing too large during multi-product searches.
/// </summary>
public interface IToolResultCompressor
{
    /// <summary>
    /// Returns a condensed version of <paramref name="rawResult"/> suitable for LLM history,
    /// or the original string when no compression is applicable.
    /// </summary>
    string Compress(string toolName, string rawResult);
}
