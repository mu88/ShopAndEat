using System.ComponentModel.DataAnnotations;

namespace ShopAndEat.Models;

public class MealModel
{
    [Required]
    public string RecipeName { get; set; }

    [Required]
    public string MealTypeName { get; set; }

    [Required]
    [FutureValidator]
    public DateTime Date { get; set; } = DateTime.Now;

    [Required]
    public int NumberOfPersons { get; set; }

    [Required]
    public int NumberOfDays { get; set; }
}

[AttributeUsage(AttributeTargets.Property)]
public class FutureValidatorAttribute : ValidationAttribute
{
    protected override ValidationResult IsValid(object value, ValidationContext validationContext)
    {
        if (value is DateTime timeStamp && timeStamp >= DateTime.Now)
        {
            return ValidationResult.Success;
        }

        return new ValidationResult("Must be in future", new[] { validationContext.MemberName });
    }
}