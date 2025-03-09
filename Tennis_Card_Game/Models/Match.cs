using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
namespace Tennis_Card_Game.Models
{
    public class Match
    {
        [Key]
        public int Id { get; set; }
        [ForeignKey("Player1")]
        public int Player1Id { get; set; }
        public virtual Player Player1 { get; set; }
        [ForeignKey("Player2")]
        public int Player2Id { get; set; }
        public virtual Player Player2 { get; set; }
        [ForeignKey("Surface")]
        public int SurfaceId { get; set; }
        public virtual Surface Surface { get; set; }
        [ForeignKey("WeatherCondition")]
        public int? WeatherConditionId { get; set; }
        public virtual WeatherCondition WeatherCondition { get; set; }
        [ForeignKey("Tournament")]
        public int? TournamentId { get; set; }
        public virtual Tournament Tournament { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public int Player1Sets { get; set; }
        public int Player2Sets { get; set; }
        public bool IsCompleted { get; set; }
        public int? WinnerId { get; set; }
        public int Round { get; set; } 
        public int MatchOrder { get; set; } 
        public virtual ICollection<Set> Sets { get; set; }

        public Match()
        {
            this.Sets = new HashSet<Set>();
            this.StartTime = DateTime.Now;
            this.IsCompleted = false;
            this.Player1Sets = 0;
            this.Player2Sets = 0;
            this.Player1 = new Player();
            this.Player2 = new Player();
            this.Surface = new Surface();
            this.WeatherCondition = new WeatherCondition();
            this.Tournament = new Tournament();
            this.Round = 1; 
            this.MatchOrder = 0; 
        }

        public Match(Player player1, Player player2, Surface surface)
            : this()
        {
            this.Player1 = player1;
            this.Player1Id = player1.Id;
            this.Player2 = player2;
            this.Player2Id = player2.Id;
            this.Surface = surface;
            this.SurfaceId = surface.Id;
        }
    }
}