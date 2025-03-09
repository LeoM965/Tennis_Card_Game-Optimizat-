using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace Tennis_Card_Game.Models
{
    public class Point
    {
        [Key]
        public int Id { get; set; }

        [ForeignKey("Game")]
        public int GameId { get; set; }
        public virtual Game Game { get; set; }

        public int PointNumber { get; set; }

        public int? WinnerId { get; set; }

        public virtual ICollection<PlayedCard> PlayedCards { get; set; }

        public Point()
        {
            this.PlayedCards = new HashSet<PlayedCard>();

            this.Game = new Game(); 
        }

        public Point(Game game, int pointNumber)
            : this() 
        {
            this.Game = game;
            this.GameId = game.Id;
            this.PointNumber = pointNumber;
        }
    }
}