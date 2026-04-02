using Microsoft.AspNetCore.Identity;

namespace Recipe_Hub.Models;

public class Comment
{
    public int Id { get; set; }
    public string Content { get; set; }

    public string UserId { get; set; }
    public IdentityUser User { get; set; }

    public int RecipeId { get; set; }
    public Recipe Recipe { get; set; }
}