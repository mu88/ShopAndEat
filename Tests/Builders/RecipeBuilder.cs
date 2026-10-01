using DataLayer.EfClasses;

namespace Tests.Builders;

/// <summary>Test-data builder for <see cref="Recipe"/>. Use <c>new RecipeBuilder().WithDefaults().Build()</c>.</summary>
public class RecipeBuilder
{
    private string _name = string.Empty;
    private int _numberOfDays;
    private int _numberOfPersons;
    private List<Ingredient> _ingredients = [];

    public RecipeBuilder WithDefaults()
    {
        _name = "Salad";
        _numberOfDays = 1;
        _numberOfPersons = 2;
        _ingredients = [new IngredientBuilder().WithDefaults().Build()];
        return this;
    }

    public Recipe Build() => new(_name, _numberOfDays, _numberOfPersons, _ingredients);
}
