using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace Tennis_Card_Game.Models
{
    public class CardSynergy
    {
        [Key]
        public int Id { get; set; }

        [ForeignKey("Card1")]
        public int Card1Id { get; set; }

        public virtual Card Card1 { get; set; }

        [ForeignKey("Card2")]
        public int Card2Id { get; set; }

        public virtual Card Card2 { get; set; }

        public int BonusPercentage { get; set; }

        [StringLength(255)]
        public string Description { get; set; }

        public CardSynergy()
        {
            this.BonusPercentage = 0;
            this.Description = string.Empty;
            this.Card1 = new Card();
            this.Card2 = new Card(); 
        }

        public CardSynergy(Card card1, Card card2, int bonusPercentage, string description)
        {
            this.Card1 = card1;
            this.Card1Id = card1.Id;
            this.Card2 = card2;
            this.Card2Id = card2.Id;
            this.BonusPercentage = bonusPercentage;
            this.Description = description;
        }
    }
}