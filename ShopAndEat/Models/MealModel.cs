using System.ComponentModel.DataAnnotations;

namespace ShopAndEat.Models;

public class MealModel
{
    public MealModel()
    {
        Date = DateTime.Now;
    }

    [Required] public string RecipeName { get; set; }

    [Required] public string MealTypeName { get; set; }

    [Required] [FutureValidator] public DateTime Date { get; set; }
    
    [Required] public int NumberOfPersons { get; set; }
    [Required] public int NumberOfDays { get; set; }
}

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