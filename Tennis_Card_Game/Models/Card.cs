using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace Tennis_Card_Game.Models
{
    public class Card
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        public string Name { get; set; }

        [StringLength(500)]
        public string Description { get; set; }

        [ForeignKey("CardCategory")]
        public int CardCategoryId { get; set; }
        public virtual CardCategory CardCategory { get; set; }

        public int Power { get; set; }
        public int Precision { get; set; }
        public int EnergyConsumption { get; set; }

        [StringLength(50)]
        public string SpecialEffect { get; set; }

        public int ClayBonus { get; set; }
        public int GrassBonus { get; set; }
        public int HardCourtBonus { get; set; }

        public bool IsWildCard { get; set; }

        public virtual ICollection<CardSynergy> SynergiesAsCard1 { get; set; }
        public virtual ICollection<CardSynergy> SynergiesAsCard2 { get; set; }

        public Card()
        {
            SynergiesAsCard1 = new HashSet<CardSynergy>();
            SynergiesAsCard2 = new HashSet<CardSynergy>();
            IsWildCard = false;
            ClayBonus = 0;
            GrassBonus = 0;
            HardCourtBonus = 0;
            SpecialEffect = string.Empty;
            Description = string.Empty;
            Name = string.Empty;
            Power = 0;
            Precision = 0;
            EnergyConsumption = 0;
            CardCategoryId = 0;
            CardCategory = new CardCategory(); 
        }

        public Card(string name, CardCategory cardCategory, int power, int precision, int energyConsumption)
: this()
        {
            Name = name;
            CardCategory = cardCategory;
            CardCategoryId = cardCategory.Id; 
            Power = power;
            Precision = precision;
            EnergyConsumption = energyConsumption;
        }
    }
}