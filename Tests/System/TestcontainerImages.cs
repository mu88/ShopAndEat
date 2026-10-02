using System.Text.Json;
using System.Text.Json.Serialization;

namespace Tests.System;

internal static class TestcontainerImages
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    private static readonly Lazy<Dictionary<string, TestcontainerImageEntry>> Images =
        new(LoadImages);

    public static string GetImageReference(string name)
    {
        if (!Images.Value.TryGetValue(name, out var entry))
        {
            throw new KeyNotFoundException($"Testcontainer image '{name}' not found in testcontainers.json");
        }

        return $"{entry.Image}:{entry.Tag}";
    }

    private static Dictionary<string, TestcontainerImageEntry> LoadImages()
    {
        var jsonPath = Path.Combine(AppContext.BaseDirectory, "testData", "system", "testcontainers.json");
        var json = File.ReadAllText(jsonPath);
        var data = JsonSerializer.Deserialize<Dictionary<string, TestcontainerImageEntry>>(json, JsonOptions);
        return data ?? new Dictionary<string, TestcontainerImageEntry>();
    }

    private sealed record TestcontainerImageEntry(string Image, string Tag);
}
