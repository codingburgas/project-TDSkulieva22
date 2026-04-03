namespace Recipe_Hub.Models;

public class RecipeImage : BaseEntity
{
    public int RecipeId { get; set; }
    public string ImagePath { get; set; }

    public Recipe Recipe { get; set; }
}