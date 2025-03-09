using System.ComponentModel.DataAnnotations;

namespace Tennis_Card_Game.Models
{
    public class WeatherCondition
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        public string Name { get; set; } // "Sunny", "Rainy", "Windy", "Hot"

        [StringLength(500)]
        public string Description { get; set; }

        public int SpeedModifier { get; set; }
        public int EnergyConsumptionModifier { get; set; }

        public virtual ICollection<Match> Matches { get; set; }

        public WeatherCondition()
        {
            this.Matches = new HashSet<Match>();
            this.SpeedModifier = 0;
            this.EnergyConsumptionModifier = 0;

            this.Name = string.Empty; 
            this.Description = string.Empty; 
        }

        // Constructor cu parametri
        public WeatherCondition(string name, int speedModifier, int energyConsumptionModifier)
            : this()
        { 
            this.Name = name;
            this.SpeedModifier = speedModifier;
            this.EnergyConsumptionModifier = energyConsumptionModifier;
        }
    }
}