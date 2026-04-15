using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Recipe_Hub.Data;
using Recipe_Hub.Models;
using Microsoft.AspNetCore.Identity;
using Ganss.Xss;

namespace Recipe_Hub.Controllers;

public class CommentController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<IdentityUser> _userManager;  //Handles user information
    private readonly HtmlSanitizer _sanitizer = new HtmlSanitizer();
    public CommentController(ApplicationDbContext context,  UserManager<IdentityUser> userManager)
    {
        _context = context;
        _userManager = userManager;
        
        _sanitizer.AllowedTags.Add("b");
        _sanitizer.AllowedTags.Add("i");
        _sanitizer.AllowedTags.Add("strong");
        _sanitizer.AllowedTags.Add("em");
        _sanitizer.AllowedTags.Add("ul");
        _sanitizer.AllowedTags.Add("li");
        _sanitizer.AllowedTags.Add("p");
        _sanitizer.AllowedTags.Add("br");
    }
    
    [Authorize]
    [HttpPost]
    public async Task<IActionResult> AddComment(int recipeId, string text)
    {
        //Prevent empty comments
        if (string.IsNullOrWhiteSpace(text))  
        {
            return RedirectToAction("Details", new { id = recipeId });
        }

        var userId = _userManager.GetUserId(User);

        //If user is not logged in
        if (userId == null)
        {
            return Unauthorized();
        }

        var comment = new Comment
        {
            Content = _sanitizer.Sanitize(text),
            RecipeId = recipeId,
            UserId = userId,
            CreatedAt = DateTime.UtcNow
        };

        //Add to database
        _context.Comments.Add(comment);
        await _context.SaveChangesAsync();

        //Reload recipe page
        return RedirectToAction("Details", "Recipes", new { id = recipeId });
    }

    [Authorize]
    [HttpPost]
    public async Task<IActionResult> Delete(int id)
    {
        //Find comment by ID
        var comment = await _context.Comments.FindAsync(id);

        if (comment == null)
            return NotFound();

        //Save recipe ID for redirect
        int recipeId = comment.RecipeId;

        _context.Comments.Remove(comment);
        await _context.SaveChangesAsync();

        //Back to recipe page
        return RedirectToAction("Details", "Recipes", new { id = recipeId });
    }
}