namespace DTO.ArticleGroup;

public class DeleteArticleGroupDto(int articleGroupId)
{
    public int ArticleGroupId { get; } = articleGroupId;
}