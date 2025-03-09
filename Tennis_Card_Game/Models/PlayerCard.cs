using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace Tennis_Card_Game.Models
{
    public class PlayerCard
    {
        [Key]
        public int Id { get; set; }

        [ForeignKey("Player")]
        public int PlayerId { get; set; }
        public virtual Player Player { get; set; }

        [ForeignKey("Card")]
        public int CardId { get; set; }
        public virtual Card Card { get; set; }

        public bool InDeck { get; set; } // Dacă cartea este în pachetul activ al jucătorului
        public bool InHand { get; set; } // Dacă cartea este în mâna curentă a jucătorului

        public PlayerCard()
        {
            this.InDeck = true;
            this.InHand = false;

            this.Player = new Player(); 
            this.Card = new Card(); 
        }

        // Constructor cu parametri
        public PlayerCard(Player player, Card card)
            : this() 
        {
            this.Player = player;
            this.PlayerId = player.Id;
            this.Card = card;
            this.CardId = card.Id;
        }
    }
}