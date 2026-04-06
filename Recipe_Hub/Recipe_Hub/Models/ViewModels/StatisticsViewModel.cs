namespace Recipe_Hub.Models.ViewModels;

public class StatisticsViewModel
{
    public IEnumerable<Recipe> TopRecipes { get; set; }
    public IEnumerable<ActiveUserViewModel> MostActiveUsers { get; set; }
    public Dictionary<string, int> CategoryPopularity { get; set; }
}