namespace Recipe_Hub.Models.ViewModels;

public class StatisticsViewModel
{
    public IEnumerable<Recipe> TopRecipes { get; set; }
    public IEnumerable<(string UserName, int RecipeCount)> MostActiveUsers { get; set; }
    public Dictionary<string, int> CategoryPopularity { get; set; }
}