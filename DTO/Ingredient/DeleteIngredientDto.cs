namespace DTO.Ingredient;

public class DeleteIngredientDto(int ingredientId)
{
    public int IngredientId { get; } = ingredientId;
}
