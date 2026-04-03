using Microsoft.AspNetCore.Mvc;
using Recipe_Hub.Services;

namespace Recipe_Hub.Controllers;

public class RecipesController : Controller
{
    private readonly IRecipeService _recipeService;

    public RecipesController(IRecipeService recipeService)
    {
        _recipeService = recipeService;
    }

    public async Task<IActionResult> Index()
    {
        var recipes = await _recipeService.GetAllAsync();
        return View(recipes);
    }

    public async Task<IActionResult> Details(int id)
    {
        var recipe = await _recipeService.GetByIdAsync(id);
        if (recipe == null)
        {
            return NotFound();
        }

        return View(recipe);
    }
}