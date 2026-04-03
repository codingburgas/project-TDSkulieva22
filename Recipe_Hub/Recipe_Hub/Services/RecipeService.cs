using Microsoft.EntityFrameworkCore;
using Recipe_Hub.Data;
using Recipe_Hub.Models;

namespace Recipe_Hub.Services;

public class RecipeService : IRecipeService
{
    private readonly ApplicationDbContext _context;

        public RecipeService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Recipe>> GetAllAsync()
        {
            return await _context.Recipes
                .Include(r => r.Category)
                .Include(r => r.Likes)
                .Include(r => r.RecipeIngredients)
                    .ThenInclude(ri => ri.Ingredient)
                .ToListAsync();
        }

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

        public async Task DeleteAsync(int id)
        {
            var recipe = await _context.Recipes.FindAsync(id);
            if (recipe != null)
            {
                _context.Recipes.Remove(recipe);
                await _context.SaveChangesAsync();
            }
        }

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
                .OrderByDescending(r => r.Likes.Count)
                .Take(3)
                .ToListAsync();
        }

        public async Task<IEnumerable<(string UserId, int RecipeCount)>> GetMostActiveUsersAsync()
        {
            return await _context.Recipes
                .GroupBy(r => r.UserId)
                .Select(g => new ValueTuple<string, int>(g.Key, g.Count()))
                .OrderByDescending(x => x.Item2)
                .ToListAsync();
        }
}