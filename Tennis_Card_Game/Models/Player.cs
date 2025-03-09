using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace Tennis_Card_Game.Models
{
    public class Player
    {
        [Key]
        public int Id { get; set; }
        public string Name { get; set; }
        public int PlayingStyleId { get; set; }
        public int? SpecialAbilityId { get; set; }
        public int Level { get; set; }
        public int Experience { get; set; }
        public int MaxEnergy { get; set; }
        public int CurrentEnergy { get; set; }
        [ForeignKey("PlayingStyleId")]
        public virtual PlayingStyle PlayingStyle { get; set; }
        [ForeignKey("SpecialAbilityId")]
        public virtual SpecialAbility SpecialAbility { get; set; }
        public bool SpecialAbilityUsed { get; set; }
        public string Momentum { get; set; }
        public string? UserId { get; set; }
        public bool IsAI { get; set; }
        public ApplicationUser User { get; set; }
        public virtual ICollection<PlayerCard> PlayerCards { get; set; }
        public virtual ICollection<Match> MatchesAsPlayer1 { get; set; }
        public virtual ICollection<Match> MatchesAsPlayer2 { get; set; }
        public Player()
        {
            PlayerCards = new HashSet<PlayerCard>();
            MatchesAsPlayer1 = new HashSet<Match>();
            MatchesAsPlayer2 = new HashSet<Match>();
            Level = 1;
            Experience = 0;
            MaxEnergy = 100;
            CurrentEnergy = 100;
            Momentum = "Neutral";
            SpecialAbilityUsed = false;
            Name = string.Empty;
            UserId = null; 
            IsAI = false;  
        }
    }
}