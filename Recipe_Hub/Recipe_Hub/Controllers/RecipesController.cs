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
using System.Security.Claims;

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

    public async Task<IActionResult> Index(string category, string time, string difficulty, string search)
    {
        ViewData["BodyClass"] = "recipes-page";
        
        ViewBag.Categories = _context.Categories.ToList();
        
        var recipes = _context.Recipes
            .Include(r => r.User)
            .Include(r => r.Likes)
            .Include(r => r.Category)
            .AsQueryable();

        if (!string.IsNullOrEmpty(category))
        {
            if (int.TryParse(category, out int catId))
            {
                recipes = recipes.Where(r => r.CategoryId == catId);
            }
        }

        if (!string.IsNullOrEmpty(time))
        {
            if (int.TryParse(time, out int t))
            {
                recipes = recipes.Where(r => r.CookingTime <= t);
            }
        }

        if (!string.IsNullOrEmpty(difficulty))
        {
            if (Enum.TryParse<DifficultyLevel>(difficulty, out var diffEnum))
            {
                recipes = recipes.Where(r => r.Difficulty == diffEnum);
            }
        }

        if (!string.IsNullOrEmpty(search))
        {
            var terms = search.ToLower().Split(' ', StringSplitOptions.RemoveEmptyEntries);

            recipes = recipes.Where(r =>
                terms.Any(term => r.Title.ToLower().Contains(term))
            );
        }

        return View(recipes.ToList());
    }

    public async Task<IActionResult> Details(int id)
    {
        var recipe = await _context.Recipes
            .Include(r => r.User)
            .Include(r => r.Category)
            .Include(r => r.RecipeIngredients).ThenInclude(ri => ri.Ingredient)
            .Include(r => r.Steps)
            .Include(r => r.Likes)
            .Include(r => r.Comments).ThenInclude(c => c.User)
            .Include(r => r.GalleryImages)
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
    
    [Authorize]
    public async Task<IActionResult> Edit(int id)
    {
        var recipe = await _context.Recipes
            .Include(r => r.RecipeIngredients)
            .Include(r => r.Steps)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (recipe == null)
            return NotFound();

        if (recipe.UserId != _userManager.GetUserId(User))
            return Forbid();

        var vm = new CreateRecipeViewModel
        {
            Title = recipe.Title,
            Description = recipe.Description,
            CookingTime = recipe.CookingTime,
            Difficulty = recipe.Difficulty,
            CategoryId = recipe.CategoryId,
            IngredientIds = recipe.RecipeIngredients.Select(ri => ri.IngredientId).ToList(),
            Quantities = recipe.RecipeIngredients.Select(ri => ri.Quantity).ToList(),
            Steps = recipe.Steps.OrderBy(s => s.StepNumber).Select(s => s.Description).ToList(),

            Categories = _context.Categories
                .Select(c => new SelectListItem { Value = c.Id.ToString(), Text = c.Name })
                .ToList(),

            Ingredients = _context.Ingredients
                .Select(i => new SelectListItem { Value = i.Id.ToString(), Text = i.Name })
                .ToList()
        };

        return View(vm);
    }
    
    [Authorize]
    [HttpPost]
    public async Task<IActionResult> Edit(int id, CreateRecipeViewModel model)
    {
        
        var recipe = await _context.Recipes
            .Include(r => r.RecipeIngredients)
            .Include(r => r.Steps)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (recipe == null)
            return NotFound();

        if (recipe.UserId != _userManager.GetUserId(User))
            return Forbid();

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

        recipe.Title = model.Title;
        recipe.Description = model.Description;
        recipe.CookingTime = model.CookingTime;
        recipe.Difficulty = model.Difficulty;
        recipe.CategoryId = model.CategoryId;

        _context.RecipeIngredients.RemoveRange(recipe.RecipeIngredients);
        recipe.RecipeIngredients = new List<RecipeIngredient>();
        
        for (int i = 0; i < model.IngredientIds.Count; i++)
        {
            if (model.IngredientIds[i] == 0) continue;

            recipe.RecipeIngredients.Add(new RecipeIngredient
            {
                IngredientId = model.IngredientIds[i],
                Quantity = model.Quantities[i]
            });
        }

        recipe.Steps.Clear();
        for (int i = 0; i < model.Steps.Count; i++)
        {
            recipe.Steps.Add(new RecipeStep
            {
                StepNumber = i + 1,
                Description = model.Steps[i]
            });
        }

        await _context.SaveChangesAsync();

        TempData["Success"] = "Recipe updated successfully!";
        return RedirectToAction(nameof(Details), new { id = recipe.Id });
    }
    
    [HttpPost]
    public IActionResult Like(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null)
            return Unauthorized();

        var recipe = _context.Recipes
            .Include(r => r.Likes)
            .FirstOrDefault(r => r.Id == id);

        if (recipe == null)
            return NotFound();

        var existingLike = recipe.Likes.FirstOrDefault(l => l.UserId == userId);

        bool liked;

        if (existingLike == null)
        {
            recipe.Likes.Add(new Like
            {
                RecipeId = id,
                UserId = userId
            });
            liked = true;
        }
        else
        {
            _context.Likes.Remove(existingLike);
            liked = false;
        }

        _context.SaveChanges();

        return Json(new { likes = recipe.Likes.Count, liked });
    }
    
    public IActionResult Delete(int id)
    {
        var recipe = _context.Recipes
            .FirstOrDefault(r => r.Id == id);

        if (recipe == null)
            return NotFound();

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (recipe.UserId != userId)
            return Unauthorized();

        return View(recipe);
    }
    
    [HttpPost]
    public IActionResult Delete(Recipe recipe)
    {
        var dbRecipe = _context.Recipes
            .Include(r => r.RecipeIngredients)
            .Include(r => r.Steps)
            .Include(r => r.Likes)
            .FirstOrDefault(r => r.Id == recipe.Id);

        if (dbRecipe == null)
            return NotFound();

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (dbRecipe.UserId != userId)
            return Unauthorized();

        _context.RecipeIngredients.RemoveRange(dbRecipe.RecipeIngredients);
        _context.RecipeSteps.RemoveRange(dbRecipe.Steps);
        _context.Likes.RemoveRange(dbRecipe.Likes);

        _context.Recipes.Remove(dbRecipe);
        _context.SaveChanges();

        return RedirectToAction("Index");
    }
}

