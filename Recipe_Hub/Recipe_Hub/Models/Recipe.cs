using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace Recipe_Hub.Models
{
    public enum DifficultyLevel
    {
        Easy = 1,
        Medium = 2,
        Hard = 3
    }

    public class Recipe : BaseEntity
    {
        [Required]
        public string Title { get; set; }

        public string Description { get; set; }

        public int CookingTime { get; set; }

        public DifficultyLevel Difficulty { get; set; }

        // FK към Category
        public int CategoryId { get; set; }
        public Category Category { get; set; }

        // FK към User (IdentityUser)
        public string UserId { get; set; }
        public IdentityUser User { get; set; }

        public string MainImagePath { get; set; } = "/Resources/Images/default.png";
        
        // Navigation properties
        public ICollection<Comment> Comments { get; set; } = new List<Comment>();
        public ICollection<RecipeIngredient> RecipeIngredients { get; set; } = new List<RecipeIngredient>();
        public ICollection<Like> Likes { get; set; } = new List<Like>();
        public ICollection<RecipeStep> Steps { get; set; } = new List<RecipeStep>();
        public ICollection<RecipeImage> GalleryImages { get; set; } = new List<RecipeImage>();
    }
}