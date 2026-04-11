using Microsoft.EntityFrameworkCore;
using Recipe_Hub.Data;
using Recipe_Hub.Models;
using Recipe_Hub.Models.ViewModels;

namespace Recipe_Hub.Services;

public class RecipeService : IRecipeService
{
    private readonly ApplicationDbContext _context;

        public RecipeService(ApplicationDbContext context)
        {
            _context = context;
        }

        //Get all recipes
        public async Task<IEnumerable<Recipe>> GetAllAsync()
        {
            return await _context.Recipes
                .Include(r => r.Category)
                .Include(r => r.Likes)
                .Include(r => r.RecipeIngredients)
                    .ThenInclude(ri => ri.Ingredient)
                .ToListAsync();
        }

        //Get a single recipe by ID with full details
        public async Task<Recipe?> GetByIdAsync(int id)
        {
            return await _context.Recipes
                .Include(r => r.Category)
                .Include(r => r.Comments)
                .Include(r => r.Likes)
                .Include(r => r.RecipeIngredients)
                    .ThenInclude(ri => ri.Ingredient)
                .FirstOrDefaultAsync(r => r.Id == id);
        }

        public async Task CreateAsync(Recipe recipe)
        {
            _context.Recipes.Add(recipe);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(Recipe recipe)
        {
            _context.Recipes.Update(recipe);
            await _context.SaveChangesAsync();
        }

        //Delete recipe by ID if it exists
        public async Task DeleteAsync(int id)
        {
            var recipe = await _context.Recipes.FindAsync(id);
            if (recipe != null)
            {
                _context.Recipes.Remove(recipe);
                await _context.SaveChangesAsync();
            }
        }

        // Filters
        public async Task<IEnumerable<Recipe>> FilterByCategoryAsync(int categoryId)
        {
            return await _context.Recipes
                .Where(r => r.CategoryId == categoryId)
                .ToListAsync();
        }

        public async Task<IEnumerable<Recipe>> FilterByDifficultyAsync(DifficultyLevel difficulty)
        {
            return await _context.Recipes
                .Where(r => r.Difficulty == difficulty)
                .ToListAsync();
        }

        public async Task<IEnumerable<Recipe>> FilterByCookingTimeAsync(int maxMinutes)
        {
            return await _context.Recipes
                .Where(r => r.CookingTime <= maxMinutes)
                .ToListAsync();
        }

        public async Task<IEnumerable<Recipe>> GetTop3MostLikedAsync()
        {
            return await _context.Recipes
                .Include(r => r.User)
                .Include(r => r.Likes)
                .Include(r => r.Comments)
                .OrderByDescending(r => r.Likes.Count)
                .Take(3)
                .ToListAsync();
        }

        //Load all recipes with user data
        public async Task<IEnumerable<ActiveUserViewModel>> GetMostActiveUsersAsync()
        {
            var recipes = await _context.Recipes
                .Include(r => r.User)
                .Include(r => r.Likes)
                .Include(r => r.Comments)
                .ToListAsync();

            //Group recipes by user and calculate activity stats
            var users = recipes
                .GroupBy(r => new { r.UserId, r.User.UserName })
                .Select(g =>
                {
                    var topRecipe = g
                        .OrderByDescending(r => r.Likes.Count)
                        .FirstOrDefault();

                    return new ActiveUserViewModel
                    {
                        UserName = g.Key.UserName,
                        RecipeCount = g.Count(),
                        TotalLikes = g.Sum(r => r.Likes.Count),
                        TotalComments = g.Sum(r => r.Comments.Count),

                        TopRecipeTitle = topRecipe?.Title ?? "No recipes",
                        TopRecipeLikes = topRecipe?.Likes.Count ?? 0,

                        ActivityScore =
                            g.Count() * 2 +
                            g.Sum(r => r.Likes.Count) +
                            g.Sum(r => r.Comments.Count)
                    };
                })
                .OrderByDescending(u => u.ActivityScore)
                .Take(3)
                .ToList();

            return users;
        }
        
        public async Task<Dictionary<string, int>> GetCategoryPopularityAsync()
        {
            //Count how many recipes belong to each category
            return await _context.Recipes
                .GroupBy(r => r.Category.Name)
                .Select(g => new { Category = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.Category, x => x.Count);
        }
}