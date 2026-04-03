using Recipe_Hub.Models;

namespace Recipe_Hub.Services;

public interface IRecipeService
{
    Task<IEnumerable<Recipe>> GetAllAsync();
    Task<Recipe?> GetByIdAsync(int id);
    Task CreateAsync(Recipe recipe);
    Task UpdateAsync(Recipe recipe);
    Task DeleteAsync(int id);
    
    Task<IEnumerable<Recipe>> FilterByCategoryAsync(int categoryId);
    Task<IEnumerable<Recipe>> FilterByDifficultyAsync(DifficultyLevel difficulty);
    Task<IEnumerable<Recipe>> FilterByCookingTimeAsync(int maxMinutes);

    Task<IEnumerable<Recipe>> GetTop3MostLikedAsync();
    Task<IEnumerable<(string UserId, int RecipeCount)>> GetMostActiveUsersAsync();
}
