using JetBrains.Annotations;

namespace DataLayer.EfClasses;

public class Meal : IHasId<MealId>
{
    // MA0056: assign the backing fields directly (not the virtual properties) from the constructor.
    private MealType _mealType;
    private Recipe _recipe;

    public Meal(DateTime day, MealType mealType, Recipe recipe, int numberOfPersons)
    {
        Day = day;
        _mealType = mealType;
        _recipe = recipe;
        NumberOfPersons = numberOfPersons;
    }

    // Stryker disable once all: these null-forgiving assignments only satisfy the compiler for EF Core materialization; removing them leaves the same runtime state.
    public Meal(int numberOfPersons)
    {
        NumberOfPersons = numberOfPersons;
        _mealType = null!;
        _recipe = null!;
    }

    public DateTime Day
    {
        get;
        [UsedImplicitly]
        private set;
    }

    public virtual MealType MealType
    {
        get => _mealType;
        [UsedImplicitly]
        private set => _mealType = value;
    }

    public virtual Recipe Recipe
    {
        get => _recipe;
        [UsedImplicitly]
        private set => _recipe = value;
    }

    public MealId MealId
    {
        get;
        [UsedImplicitly]
        private set;
    }

    public bool HasBeenShopped { get; private set; }

    public int NumberOfPersons { get; private set; }

    /// <summary>Marks this meal as shopped for.</summary>
    public void MarkAsShopped() => HasBeenShopped = true;

    /// <summary>Toggles whether this meal has been shopped for.</summary>
    public void ToggleShopped() => HasBeenShopped = !HasBeenShopped;
}
