namespace Recipe_Hub.Models;

public class Category : BaseEntity
{
    public string Name { get; set; }
    public ICollection<Recipe> Recipes { get; set; } = new List<Recipe>();
}