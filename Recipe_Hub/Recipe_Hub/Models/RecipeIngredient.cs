namespace Recipe_Hub.Models;

public class RecipeIngredient
{
    public int Id { get; set; }
    public int RecipeId { get; set; }
    public Recipe Recipe { get; set; }  //Navigation property

    public int IngredientId { get; set; }
    public Ingredient Ingredient { get; set; }  //Navigation property
    
    public string Quantity { get; set; }
}