using DataLayer.EfClasses;

namespace Tests.Builders;

/// <summary>Test-data builder for <see cref="Meal"/>. Use <c>new MealBuilder().WithDefaults().Build()</c>.</summary>
public class MealBuilder
{
    private readonly List<Action<Meal>> _postBuildActions = [];
    private DateTime _day;
    private MealType _mealType = null!;
    private Recipe _recipe = null!;
    private int _numberOfPersons;

    public MealBuilder WithDefaults()
    {
        _day = new DateTime(2024, 1, 15);
        _mealType = new MealTypeBuilder().WithDefaults().Build();
        _recipe = new RecipeBuilder().WithDefaults().Build();
        _numberOfPersons = 2;
        _postBuildActions.Clear();
        return this;
    }

    /// <summary>Builds a meal that has already been marked as shopped, using the real <see cref="Meal.MarkAsShopped"/> business method.</summary>
    public MealBuilder AsShopped()
    {
        _postBuildActions.Add(meal => meal.MarkAsShopped());
        return this;
    }

    public Meal Build()
    {
        var meal = new Meal(_day, _mealType, _recipe, _numberOfPersons);
        foreach (var postBuildAction in _postBuildActions)
        {
            postBuildAction(meal);
        }

        return meal;
    }
}
