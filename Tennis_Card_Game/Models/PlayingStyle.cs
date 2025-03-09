using System.ComponentModel.DataAnnotations;

namespace Tennis_Card_Game.Models
{
    public class PlayingStyle
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        public string Name { get; set; } // "Aggressive", "Defensive", "All-Court", etc.

        [StringLength(500)]
        public string Description { get; set; }

        public int OffensiveBonus { get; set; }
        public int DefensiveBonus { get; set; }
        public int EnergyEfficiency { get; set; }

        public virtual ICollection<Player> Players { get; set; }

        public PlayingStyle()
        {
            this.Players = new HashSet<Player>();

            this.Name = string.Empty; 
            this.Description = string.Empty; 
        }

        public PlayingStyle(string name, int offensiveBonus, int defensiveBonus, int energyEfficiency)
            : this() 
        {
            this.Name = name;
            this.OffensiveBonus = offensiveBonus;
            this.DefensiveBonus = defensiveBonus;
            this.EnergyEfficiency = energyEfficiency;
        }
    }
}