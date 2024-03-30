using DTO.ArticleGroup;

namespace DTO.Article;

public class NewArticleDto(string name, ExistingArticleGroupDto articleGroup, bool isInventory)
{
    public string Name { get; } = name;

    public ExistingArticleGroupDto ArticleGroup { get; } = articleGroup;

    public bool IsInventory { get; } = isInventory;
}