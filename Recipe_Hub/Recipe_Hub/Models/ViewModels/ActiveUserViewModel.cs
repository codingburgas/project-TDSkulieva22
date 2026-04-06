namespace Recipe_Hub.Models.ViewModels;
public class ActiveUserViewModel
{
    public string UserName { get; set; }
    public int RecipeCount { get; set; }
    public int TotalLikes { get; set; }
    public int TotalComments { get; set; }

    public string TopRecipeTitle { get; set; }
    public int TopRecipeLikes { get; set; }

    public int ActivityScore { get; set; }
}