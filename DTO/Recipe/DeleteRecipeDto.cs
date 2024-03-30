namespace DTO.Recipe;

public class DeleteRecipeDto(int recipeId)
{
    public int RecipeId { get;  } = recipeId;
}