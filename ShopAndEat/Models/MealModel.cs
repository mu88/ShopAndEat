using System;
using System.ComponentModel.DataAnnotations;

namespace ShopAndEat.Models;

public class MealModel
{
    [Required]
    public string RecipeName { get; set; }

    [Required]
    public string MealTypeName { get; set; }

    [Required]
    [TodayOrFutureValidator]
    public DateTime Date { get; set; } = DateTime.Now;

    [Required]
    public int NumberOfPersons { get; set; }

    [Required]
    public int NumberOfDays { get; set; }
}

[AttributeUsage(AttributeTargets.Property)]
public class TodayOrFutureValidatorAttribute : ValidationAttribute
{
    protected override ValidationResult IsValid(object value, ValidationContext validationContext)
    {
        if (value is DateTime timeStamp && timeStamp >= DateTime.Now.Date)
        {
            return ValidationResult.Success;
        }

        return new ValidationResult("Must be today or in future", new[] { validationContext.MemberName });
    }
}