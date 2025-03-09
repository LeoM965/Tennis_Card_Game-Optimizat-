using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace Tennis_Card_Game.Models
{
    public class Game
    {
        [Key]
        public int Id { get; set; }

        [ForeignKey("Set")]
        public int SetId { get; set; }
        public virtual Set Set { get; set; }

        public int GameNumber { get; set; }
        public int ServerId { get; set; }

        public string Player1Score { get; set; }
        public string Player2Score { get; set; }

        public bool IsCompleted { get; set; }
        public int? WinnerId { get; set; }

        public virtual ICollection<Point> Points { get; set; }

        public Game()
        {
            this.Points = new HashSet<Point>();
            this.IsCompleted = false;
            this.Player1Score = "0"; 
            this.Player2Score = "0";
            this.Set = new Set(); 
        }

        public Game(Set set, int gameNumber, int serverId)
            : this() 
        {
            this.Set = set;
            this.SetId = set.Id;
            this.GameNumber = gameNumber;
            this.ServerId = serverId;
        }
    }
}