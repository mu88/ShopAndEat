using DTO.ArticleGroup;

namespace DTO.Article;

public class ExistingArticleDto(
    int articleId,
    string name,
    ExistingArticleGroupDto articleGroup,
    bool isInventory)
{
    public int ArticleId { get; } = articleId;

    public string Name { get; } = name;

    public ExistingArticleGroupDto ArticleGroup { get; } = articleGroup;

    public bool IsInventory { get; } = isInventory;
}
