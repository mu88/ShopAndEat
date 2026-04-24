using System.Text.Json;
using System.Text.Json.Serialization;
using ShoppingAgent.Models;

namespace ShoppingAgent.Services.Concrete;

/// <summary>
/// Compresses search and product-detail results to a minimal representation
/// before they enter the LLM conversation history.
/// Only name, price, and URL are kept — the fields the LLM actually needs for planning.
/// </summary>
public class ToolResultCompressor : IToolResultCompressor
{
    private const int MaxSearchResults = 5;

    private static readonly JsonSerializerOptions ReadOptions =
        new() { PropertyNameCaseInsensitive = true };

    private static readonly JsonSerializerOptions WriteOptions =
        new() { DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull };

    public string Compress(string toolName, string rawResult)
    {
        if (string.IsNullOrWhiteSpace(rawResult))
        {
            return rawResult;
        }

        return toolName switch
        {
            "search_products" => CompressSearchResults(rawResult),
            "get_product_details" => CompressProductDetails(rawResult),
            _ => rawResult,
        };
    }

    private static string CompressSearchResults(string rawResult)
    {
        try
        {
            var products = JsonSerializer.Deserialize<List<ShopProduct>>(rawResult, ReadOptions);
            if (products is null)
            {
                return rawResult;
            }

            var slim = products
                .Take(MaxSearchResults)
                .Select(product => new { product.Name, product.Price, product.Url })
                .ToList();

            return JsonSerializer.Serialize(slim, WriteOptions);
        }
        catch (JsonException)
        {
            return rawResult;
        }
    }

    private static string CompressProductDetails(string rawResult)
    {
        try
        {
            var details = JsonSerializer.Deserialize<ProductDetails>(rawResult, ReadOptions);
            if (details is null)
            {
                return rawResult;
            }

            var slim = new { details.Name, details.Price, details.UnitSize, details.Url };
            return JsonSerializer.Serialize(slim, WriteOptions);
        }
        catch (JsonException)
        {
            return rawResult;
        }
    }
}
