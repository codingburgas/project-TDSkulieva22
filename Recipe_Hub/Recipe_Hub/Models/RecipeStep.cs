namespace Recipe_Hub.Models;

public class RecipeStep : BaseEntity
{
    public int RecipeId { get; set; }
    public Recipe Recipe { get; set; }  //Navigation property

    public int StepNumber { get; set; }
    public string Description { get; set; }
}