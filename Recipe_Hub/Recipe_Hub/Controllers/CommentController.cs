using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Recipe_Hub.Data;
using Recipe_Hub.Models;
using Microsoft.AspNetCore.Identity;

namespace Recipe_Hub.Controllers;

public class CommentController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<IdentityUser> _userManager;
    
    public CommentController(ApplicationDbContext context,  UserManager<IdentityUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }
    
    [Authorize(Roles = "Admin")]
    [HttpPost]
    public async Task<IActionResult> AddComment(int recipeId, string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return RedirectToAction("Details", new { id = recipeId });
        }

        var userId = _userManager.GetUserId(User);

        if (userId == null)
        {
            return Unauthorized();
        }

        var comment = new Comment
        {
            Content = text,
            RecipeId = recipeId,
            UserId = userId,
            CreatedAt = DateTime.UtcNow
        };

        _context.Comments.Add(comment);
        await _context.SaveChangesAsync();

        return RedirectToAction("Details", new { id = recipeId });
    }

    [Authorize(Roles = "Admin")]
    [HttpPost]
    public async Task<IActionResult> Delete(int id)
    {
        var comment = await _context.Comments.FindAsync(id);

        if (comment == null)
            return NotFound();

        int recipeId = comment.RecipeId;

        _context.Comments.Remove(comment);
        await _context.SaveChangesAsync();

        return RedirectToAction("Details", "Recipes", new { id = recipeId });
    }
}