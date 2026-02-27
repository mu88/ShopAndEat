namespace DTO.Article;

public class DeleteArticleDto(in int articleId)
{
    public int ArticleId { get; } = articleId;
}
