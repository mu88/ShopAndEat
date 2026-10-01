using System.ComponentModel.DataAnnotations;

namespace ShopAndEat.Models;

public class MealModel
{
    [Required]
    public string RecipeName { get; set; } = string.Empty;

    [Required]
    public string MealTypeName { get; set; } = string.Empty;

    // Default is set explicitly (via injected TimeProvider) by the consuming component, not here,
    // since this plain model has no access to dependency injection.
    [Required]
    [TodayOrFutureValidator]
    public DateTime Date { get; set; }

    [Required]
    public int NumberOfPersons { get; set; }

    [Required]
    public int NumberOfDays { get; set; }
}

[AttributeUsage(AttributeTargets.Property)]
public class TodayOrFutureValidatorAttribute : ValidationAttribute
{
    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        // ValidationAttribute is static metadata without direct DI support; fall back to
        // TimeProvider.System when no TimeProvider is registered in the validation context.
        var timeProvider = validationContext.GetService(typeof(TimeProvider)) as TimeProvider ?? TimeProvider.System;
        if (value is DateTime timeStamp && timeStamp >= timeProvider.GetLocalNow().DateTime.Date)
        {
            return ValidationResult.Success;
        }

        return new ValidationResult("Must be today or in future", new[] { validationContext.MemberName! });
    }
}
