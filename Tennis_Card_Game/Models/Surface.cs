using System.ComponentModel.DataAnnotations;

namespace Tennis_Card_Game.Models
{
    public class Surface
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        public string Name { get; set; } // "Clay", "Grass", "Hard Court"

        [StringLength(500)]
        public string Description { get; set; }

        public virtual ICollection<Match> Matches { get; set; }
        public Surface()
        {
            this.Matches = new HashSet<Match>();

            this.Name = string.Empty;
            this.Description = string.Empty; 
        }

        public Surface(string name, string description)
            : this() 
        {
            this.Name = name;
            this.Description = description;
        }
    }
}