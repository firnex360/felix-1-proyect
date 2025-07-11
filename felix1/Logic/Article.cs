using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace felix1.Logic
{

    public enum ArticleCategory
    {
        MainDish,
        SideDish,
        Drink,
        Dessert,
        NonFood,
        Other
    }
    public class Article
    {
        public int Id { get; set; } // Auto-generated by EF Core

        public string? Name { get; set; } = null;

        public float PriPrice { get; set; } = 0;

        public float SecPrice { get; set; } = 0;

        public ArticleCategory Category { get; set; }

        public bool IsDeleted { get; set; } = false;

        public bool IsSideDish { get; set; } = false;

        // Self-referencing relationship for side dishes
        public List<Article>? SideDishes { get; set; } = new();
    }
}
