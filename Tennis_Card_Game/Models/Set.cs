using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace Tennis_Card_Game.Models
{
    public class Set
    {
        [Key]
        public int Id { get; set; }

        [ForeignKey("Match")]
        public int MatchId { get; set; }
        public virtual Match Match { get; set; }

        public int SetNumber { get; set; }
        public int Player1Games { get; set; }
        public int Player2Games { get; set; }
        public bool IsCompleted { get; set; }
        public int? WinnerId { get; set; }

        public virtual ICollection<Game> Games { get; set; }

        public Set()
        {
            this.Games = new HashSet<Game>();
            this.IsCompleted = false;
            this.Player1Games = 0;
            this.Player2Games = 0;

            this.Match = new Match();
        }
        public Set(Match match, int setNumber)
            : this() 
        {
            this.Match = match;
            this.MatchId = match.Id;
            this.SetNumber = setNumber;
        }
    }
}