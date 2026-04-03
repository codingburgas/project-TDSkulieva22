using Microsoft.AspNetCore.Mvc;
using Recipe_Hub.Services;
using Recipe_Hub.Models;
using Microsoft.AspNetCore.Mvc.Rendering;
using Recipe_Hub.Data;
using Microsoft.AspNetCore.Identity;
using Recipe_Hub.Models.ViewModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;

namespace Recipe_Hub.Controllers;

public class RecipesController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<IdentityUser> _userManager;
    private readonly IWebHostEnvironment _env; 
    public RecipesController(ApplicationDbContext context,
        UserManager<IdentityUser> userManager, IWebHostEnvironment env)
    {
        _context = context;
        _userManager = userManager;
        _env = env;
    }

    public async Task<IActionResult> Index()
    {
        var recipes = await _context.Recipes
            .Include(r => r.Category)
            .ToListAsync();
        
        return View(recipes);
    }

    public async Task<IActionResult> Details(int id)
    {
        var recipe = await _context.Recipes
            .Include(r => r.Category)
            .Include(r => r.RecipeIngredients).ThenInclude(ri => ri.Ingredient)
            .Include(r => r.Steps)
            .FirstOrDefaultAsync(r => r.Id == id);
        
        if (recipe == null)
        {
            return NotFound();
        }

        return View(recipe);
    }
    [Authorize]
    public IActionResult Create()
    {
        var vm = new CreateRecipeViewModel
        {
            Categories = _context.Categories
                .Select(c => new SelectListItem
                {
                    Value = c.Id.ToString(),
                    Text = c.Name
                })
                .ToList(),

            Ingredients = _context.Ingredients
                .Select(i => new SelectListItem
                {
                    Value = i.Id.ToString(),
                    Text = i.Name
                })
                .ToList()
        };
        
        return View(vm);
    }
    [Authorize]
    [HttpPost]
    public async Task<IActionResult> Create(CreateRecipeViewModel model)
    {
        if (!ModelState.IsValid)
        {
            model.Categories = _context.Categories
                .Select(c => new SelectListItem { Value = c.Id.ToString(), Text = c.Name })
                .ToList();

            model.Ingredients = _context.Ingredients
                .Select(i => new SelectListItem { Value = i.Id.ToString(), Text = i.Name })
                .ToList();

            return View(model);
        }

        var recipe = new Recipe
        {
            Title = model.Title,
            Description = model.Description,
            CookingTime = model.CookingTime,
            Difficulty = model.Difficulty,
            CategoryId = model.CategoryId,
            UserId = _userManager.GetUserId(User),
            CreatedAt = DateTime.UtcNow,
            RecipeIngredients = new List<RecipeIngredient>(),
            Steps = new List<RecipeStep>(),
            GalleryImages = new List<RecipeImage>() 
        };

        _context.Recipes.Add(recipe);
        
        //Main image
        if (model.MainImage != null)
        {
            var fileName = Guid.NewGuid() + Path.GetExtension(model.MainImage.FileName);
            var uploads = Path.Combine(_env.WebRootPath, "Resources", "Images");
            Directory.CreateDirectory(uploads);

            var filePath = Path.Combine(uploads, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await model.MainImage.CopyToAsync(stream);
            }

            recipe.MainImagePath = "/Resources/Images/" + fileName;
        }
        else
        {
            recipe.MainImagePath = "/Resources/Images/default.png"; 
        }
            
        
        //Gallery
        if (model.GalleryImages != null && model.GalleryImages.Count > 0)
        {
            int count = 0;

            foreach (var image in model.GalleryImages)
            {
                if (count >= 5) break;

                var fileName = Guid.NewGuid() + Path.GetExtension(image.FileName);
                var uploads = Path.Combine(_env.WebRootPath, "Resources", "Images");
                Directory.CreateDirectory(uploads);

                var filePath = Path.Combine(uploads, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await image.CopyToAsync(stream);
                }

                recipe.GalleryImages.Add(new RecipeImage
                {
                    ImagePath = "/Resources/Images/" + fileName
                });

                count++;
            }
        }
        
        for (int i = 0; i < model.IngredientIds.Count; i++)
        {
            if (model.IngredientIds[i] == 0) continue;
            
            //var ri = new RecipeIngredient
            //{
                //RecipeId = recipe.Id,
                //IngredientId = model.IngredientIds[i],
                //Quantity = model.Quantities[i]
            //};
            //_context.RecipeIngredients.Add(ri);
            recipe.RecipeIngredients.Add(new RecipeIngredient
            {
                IngredientId = model.IngredientIds[i],
                Quantity = model.Quantities[i]
            });
        }

        for (int i = 0; i < model.Steps.Count; i++)
        {
            //var step = new RecipeStep
            //{
                //RecipeId = recipe.Id,
                //StepNumber = i + 1,
                //Description = model.Steps[i]
            //};

            //_context.RecipeSteps.Add(step);
            recipe.Steps.Add(new RecipeStep
            {
                StepNumber = i + 1,
                Description = model.Steps[i]
            });
        }
        await _context.SaveChangesAsync();
        
        TempData["Success"] = "Recipe created successfully!";
        return RedirectToAction(nameof(Index));
    }
}

