namespace Recipe_Hub.Models;

public class Ingredient : BaseEntity
{
    public string Name { get; set; }

    public ICollection<RecipeIngredient> RecipeIngredients { get; set; } = new List<RecipeIngredient>();
}