namespace DTO.ArticleGroup;

public class ExistingArticleGroupDto(int articleGroupId, string name)
{
    public int ArticleGroupId { get; } = articleGroupId;

    public string Name { get; } = name;
}