using System.ComponentModel.DataAnnotations;

namespace Tennis_Card_Game.Models
{
    public class SpecialAbility
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        public string Name { get; set; }

        [StringLength(500)]
        public string Description { get; set; }

        [StringLength(50)]
        public string Effect { get; set; } // "EnergyRestore", "PrecisionBoost", "NewCards", etc.

        public int EffectValue { get; set; }

        public virtual ICollection<Player> Players { get; set; }

        public SpecialAbility()
        {
            this.Players = new HashSet<Player>();

            this.Name = string.Empty; 
            this.Description = string.Empty; 
            this.Effect = string.Empty; 
        }

        public SpecialAbility(string name, string effect, int effectValue)
            : this() 
        {
            this.Name = name;
            this.Effect = effect;
            this.EffectValue = effectValue;
        }
    }
}