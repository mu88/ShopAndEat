using System.ComponentModel.DataAnnotations;
using DataLayer.EfClasses;

namespace DTO.ShoppingPreference;

public record PreferenceRequest
{
    [Required]
    [MaxLength(100)]
    public string Scope { get; init; } = string.Empty;

    [Required]
    [MaxLength(200)]
    public string Key { get; init; } = string.Empty;

    [Required]
    [MaxLength(10000)]
    public string Value { get; init; } = string.Empty;

    public PreferenceSource Source { get; init; } = PreferenceSource.UserConfirmed;

    [MaxLength(100)]
    public string? StoreKey { get; init; }
}

public record PreferenceResponse
{
    public string Scope { get; init; } = string.Empty;
    public string Key { get; init; } = string.Empty;
    public string Value { get; init; } = string.Empty;
    public string Source { get; init; } = string.Empty;
    public string? StoreKey { get; init; }
    public int UsageCount { get; init; }
}
