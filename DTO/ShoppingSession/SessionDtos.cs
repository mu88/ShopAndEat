#pragma warning disable SA1010 // Opening square brackets should not be preceded by a space

using System.ComponentModel.DataAnnotations;
using DataLayer.EfClasses;

namespace DTO.ShoppingSession;

public record CreateSessionRequest
{
    [Required]
    [MaxLength(50000)]
    public string IngredientList { get; init; } = string.Empty;
}

public record AddSessionItemRequest
{
    [Required]
    [MaxLength(500)]
    public string OriginalIngredient { get; init; } = string.Empty;

    [Required]
    [MaxLength(500)]
    public string SelectedProductName { get; init; } = string.Empty;

    [Required]
    [Url]
    [MaxLength(2048)]
    public string SelectedProductUrl { get; init; } = string.Empty;

    [Range(1, 10000)]
    public int Quantity { get; init; } = 1;

    [MaxLength(50)]
    public string Price { get; init; } = string.Empty;

    public SessionItemStatus Status { get; init; } = SessionItemStatus.Added;
}

public record SessionResponse
{
    public int SessionId { get; init; }

    public DateTimeOffset StartedAt { get; init; }

    public DateTimeOffset? CompletedAt { get; init; }

    public string Status { get; init; } = string.Empty;

    public string IngredientList { get; init; } = string.Empty;

    public int ItemCount { get; init; }
}

public record SessionDetailResponse
{
    public int SessionId { get; init; }

    public DateTimeOffset StartedAt { get; init; }

    public DateTimeOffset? CompletedAt { get; init; }

    public string Status { get; init; } = string.Empty;

    public string IngredientList { get; init; } = string.Empty;

    public IReadOnlyList<SessionItemResponse> Items { get; init; } = [];
}

public record SessionItemResponse
{
    public string OriginalIngredient { get; init; } = string.Empty;

    public string SelectedProductName { get; init; } = string.Empty;

    public string SelectedProductUrl { get; init; } = string.Empty;

    public int Quantity { get; init; }

    public string Price { get; init; } = string.Empty;

    public string Status { get; init; } = string.Empty;

    public DateTimeOffset AddedAt { get; init; }
}

public record CreateSessionResponse
{
    public int ShoppingSessionId { get; init; }
}

public record CreateSessionItemResponse
{
    public int ShoppingSessionItemId { get; init; }
}
