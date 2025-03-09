using System.ComponentModel.DataAnnotations;
namespace Tennis_Card_Game.Models
{
    public class CardCategory
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        public string Name { get; set; } // "Shot", "Positioning", "Strategy"

        [StringLength(50)]
        public string SubCategory { get; set; } // "Serve", "Forehand", "Backhand", etc.

        public virtual ICollection<Card> Cards { get; set; }
        public CardCategory()
        {
            this.Cards = new HashSet<Card>();
            this.Name = string.Empty; 
            this.SubCategory = string.Empty; 
        }

        public CardCategory(string name, string subCategory)
        {
            this.Name = name;
            this.SubCategory = subCategory;
            this.Cards = new HashSet<Card>();
        }
    }
}