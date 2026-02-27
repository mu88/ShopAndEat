namespace DataLayer.EfClasses;

public class Article
{
    public string Name { get; set; }

    public virtual ArticleGroup ArticleGroup { get; set; }

    public bool IsInventory { get; set; }

    public int ArticleId { get; set; }
}
