using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace Recipe_Hub.Models.ViewModels;

    public class CreateRecipeViewModel
    {
        public string Title { get; set; }
        public string Description { get; set; }
        public int CookingTime { get; set; }
        public DifficultyLevel Difficulty { get; set; }
        public int CategoryId { get; set; }

        public List<int> IngredientIds { get; set; } = new();
        public List<string> Quantities { get; set; } = new();
        
        public List<string> Steps { get; set; } = new();

        public IFormFile MainImage { get; set; }
        public List<IFormFile> GalleryImages { get; set; } = new();
        
        // Dropdown lists
        [ValidateNever]
        public IEnumerable<SelectListItem> Categories { get; set; }
        [ValidateNever]
        public IEnumerable<SelectListItem> Ingredients { get; set; }
    }
