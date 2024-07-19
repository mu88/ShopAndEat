using JetBrains.Annotations;

namespace DataLayer.EfClasses;

public class Ingredient
{
    public Ingredient(Article article, double quantity, Unit unit)
    {
        Article = article;
        Quantity = quantity;
        Unit = unit;
    }

    public Ingredient()
    {
    }

    public virtual Article Article
    {
        get;
        [UsedImplicitly]
        private set;
    }

    public double Quantity
    {
        get;
        [UsedImplicitly]
        private set;
    }

    public virtual Unit Unit
    {
        get;
        [UsedImplicitly]
        private set;
    }

    public int IngredientId
    {
        get;
        [UsedImplicitly]
        private set;
    }
}