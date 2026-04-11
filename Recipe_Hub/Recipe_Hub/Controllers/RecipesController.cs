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
    private readonly IWebHostEnvironment _env; //Access to wwwroot for file uploads
    public RecipesController(ApplicationDbContext context,
        UserManager<IdentityUser> userManager, IWebHostEnvironment env)
    {
        _context = context;
        _userManager = userManager;
        _env = env;
    }

    public async Task<IActionResult> Index(string category, string time, string difficulty, string search, string sort)
    {
        ViewData["BodyClass"] = "recipes-page";  //Add CSS class for layout
        
        ViewBag.Categories = _context.Categories.ToList();  //Load categories for filters

        
        var recipes = _context.Recipes
            .Include(r => r.User)
            .Include(r => r.Likes)
            .Include(r => r.Category)
            .AsQueryable(); //Base query with relations

        //Base query with relations
        if (!string.IsNullOrEmpty(category))
        {
            if (int.TryParse(category, out int catId))
            {
                recipes = recipes.Where(r => r.CategoryId == catId);
            }
        }

        //Filter by cooking time
        if (!string.IsNullOrEmpty(time))
        {
            if (int.TryParse(time, out int t))
            {
                recipes = recipes.Where(r => r.CookingTime <= t);
            }
        }

        //Filter by difficulty
        if (!string.IsNullOrEmpty(difficulty))
        {
            if (Enum.TryParse<DifficultyLevel>(difficulty, out var diffEnum))
            {
                recipes = recipes.Where(r => r.Difficulty == diffEnum);
            }
        }

        //Search by title
        if (!string.IsNullOrEmpty(search))
        {
            var terms = search.ToLower().Split(' ', StringSplitOptions.RemoveEmptyEntries);

            recipes = recipes.Where(r =>
                terms.Any(term => r.Title.ToLower().Contains(term))
            );
        }

        //Sorting options
        if (!string.IsNullOrEmpty(sort))
        {
            switch (sort)
            {
                case "newest":
                    recipes = recipes.OrderByDescending(r => r.CreatedAt);
                    break;

                case "likes":
                    recipes = recipes.OrderByDescending(r => r.Likes.Count);
                    break;
            }
        }

        //Return filtered list
        return View(await recipes.ToListAsync());
    }

    public async Task<IActionResult> Details(int id)
    {
        //Load full recipe with relations
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

        //Show recipe details
        return View(recipe);
    }
    
    [Authorize]
    public IActionResult Create()
    {
        var vm = new CreateRecipeViewModel
        {
            //Load categories for dropdown
            Categories = _context.Categories
                .Select(c => new SelectListItem
                {
                    Value = c.Id.ToString(),
                    Text = c.Name
                })
                .ToList(),

            //Load ingredients for selection
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
        //If validation fails, reload dropdowns
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

        //Create recipe object
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
            
        
        //Gallery image max 5
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
        
        //Add ingredients
        for (int i = 0; i < model.IngredientIds.Count; i++)
        {
            if (model.IngredientIds[i] == 0) continue;

            recipe.RecipeIngredients.Add(new RecipeIngredient
            {
                IngredientId = model.IngredientIds[i],
                Quantity = model.Quantities[i]
            });
        }

        //Add steps
        for (int i = 0; i < model.Steps.Count; i++)
        {
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
            .FirstOrDefaultAsync(r => r.Id == id); //Load recipe for editing

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

            //Dropdowns
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
        //Load recipe with ingredients and steps
        var recipe = await _context.Recipes
            .Include(r => r.RecipeIngredients)
            .Include(r => r.Steps)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (recipe == null)
            return NotFound();

        if (recipe.UserId != _userManager.GetUserId(User))
            return Forbid();

        //Reload dropdown lists if validation fails
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
        
        //Update main image if a new one is uploaded
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

        //Update basic recipe fields
        recipe.Title = model.Title;
        recipe.Description = model.Description;
        recipe.CookingTime = model.CookingTime;
        recipe.Difficulty = model.Difficulty;
        recipe.CategoryId = model.CategoryId;

        //Replace old ingredients with new ones
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
        
        //Replace old steps with new ones
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
        //Get logged-in user ID
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

        //Toggle like/unlike
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

        //Return updated like count
        return Json(new { likes = recipe.Likes.Count, liked });
    }
    
    [HttpPost]
    public IActionResult Delete(int id)
    {
        //Load recipe with all related data
        var recipe = _context.Recipes
            .Include(r => r.Likes)
            .Include(r => r.Comments)
            .Include(r => r.RecipeIngredients)
            .Include(r => r.Steps)
            .Include(r => r.GalleryImages)
            .FirstOrDefault(r => r.Id == id);

        if (recipe == null)
            return NotFound();

        //Remove related entities first
        _context.Likes.RemoveRange(recipe.Likes);
        _context.Comments.RemoveRange(recipe.Comments);
        _context.RecipeIngredients.RemoveRange(recipe.RecipeIngredients);
        _context.RecipeSteps.RemoveRange(recipe.Steps);
        _context.RecipeImages.RemoveRange(recipe.GalleryImages);
        _context.Recipes.Remove(recipe);

        _context.SaveChanges();

        return RedirectToAction("Index"); //Back to list
    }
}

