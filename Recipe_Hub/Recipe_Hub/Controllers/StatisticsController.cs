using Microsoft.AspNetCore.Mvc;
using Recipe_Hub.Services;
using Recipe_Hub.Models.ViewModels;

namespace Recipe_Hub.Controllers;

public class StatisticsController : Controller
{
    private readonly IRecipeService _recipeService;

    public StatisticsController(IRecipeService recipeService)
    {
        _recipeService = recipeService;
    }

    public async Task<IActionResult> Index()
    {
        var topRecipes = await _recipeService.GetTop3MostLikedAsync();
        var activeUsers = await _recipeService.GetMostActiveUsersAsync();
        var categoryStats = await _recipeService.GetCategoryPopularityAsync();

        var vm = new StatisticsViewModel
        {
            TopRecipes = topRecipes,
            MostActiveUsers = activeUsers,
            CategoryPopularity = categoryStats
        };

        return View(vm);
    }
}