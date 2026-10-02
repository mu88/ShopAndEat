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
            "get_cart_contents" => CompressCartContents(rawResult),
            "add_to_cart" => CompressAddToCart(rawResult),
            "remove_from_cart" => CompressRemoveFromCart(rawResult),
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

    private static string CompressCartContents(string rawResult)
    {
        try
        {
            if (rawResult.StartsWith('['))
            {
                // The StartsWith check above guarantees non-empty, array-shaped JSON, so Deserialize never returns null here.
                var items = JsonSerializer.Deserialize<List<Dictionary<string, object>>>(rawResult, ReadOptions)!;

                var slim = items
                    .Select(item => new
                    {
                        name = item.TryGetValue("name", out var name) ? name?.ToString() : null,
                        qty = item.TryGetValue("qty", out var qty) ? qty?.ToString() : null,
                        price = item.TryGetValue("price", out var price) ? price?.ToString() : null,
                    })
                    .ToList();

                return JsonSerializer.Serialize(slim, WriteOptions);
            }

            return rawResult;
        }
        catch (JsonException)
        {
            return rawResult;
        }
    }

    private static string CompressAddToCart(string rawResult) => CompressCartMutationResult(rawResult);

    private static string CompressRemoveFromCart(string rawResult) => CompressCartMutationResult(rawResult);

    /// <summary>
    /// Shared compression logic for cart-mutation tool results (add_to_cart, remove_from_cart),
    /// which both only need to keep the "success" and "message" fields.
    /// </summary>
    private static string CompressCartMutationResult(string rawResult)
    {
        try
        {
            if (rawResult.StartsWith('{'))
            {
                // The StartsWith check above guarantees non-empty, object-shaped JSON, so Deserialize never returns null here.
                var result = JsonSerializer.Deserialize<Dictionary<string, object>>(rawResult, ReadOptions)!;

                var slim = new
                {
                    success = result.TryGetValue("success", out var success) ? success?.ToString() : null,
                    message = result.TryGetValue("message", out var message) ? message?.ToString() : null,
                };

                return JsonSerializer.Serialize(slim, WriteOptions);
            }

            return rawResult;
        }
        catch (JsonException)
        {
            return rawResult;
        }
    }
}
