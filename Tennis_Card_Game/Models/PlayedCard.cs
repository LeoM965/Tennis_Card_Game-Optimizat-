using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace Tennis_Card_Game.Models
{
    public class PlayedCard
    {
        [Key]
        public int Id { get; set; }

        [ForeignKey("Point")]
        public int PointId { get; set; }
        public virtual Point Point { get; set; }

        [ForeignKey("Player")]
        public int PlayerId { get; set; }
        public virtual Player Player { get; set; }

        [ForeignKey("Card")]
        public int CardId { get; set; }
        public virtual Card Card { get; set; }

        public int PlayOrder { get; set; }
        public int EffectivePower { get; set; }
        public int EffectivePrecision { get; set; }

        public PlayedCard()
        {
            this.EffectivePower = 0;
            this.EffectivePrecision = 0;

            this.Point = new Point(); 
            this.Player = new Player(); 
            this.Card = new Card(); 
        }

        public PlayedCard(Point point, Player player, Card card, int playOrder)
            : this()
        { 
            this.Point = point;
            this.PointId = point.Id;
            this.Player = player;
            this.PlayerId = player.Id;
            this.Card = card;
            this.CardId = card.Id;
            this.PlayOrder = playOrder;

            this.EffectivePower = card.Power;
            this.EffectivePrecision = card.Precision;
        }
    }
}