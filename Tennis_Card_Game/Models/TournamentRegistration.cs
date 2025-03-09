using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Tennis_Card_Game.Models
{
    public class TournamentRegistration
    {
        [Key]
        public int Id { get; set; }

        public int TournamentId { get; set; }

        public int PlayerId { get; set; }

        [Required]
        public DateTime RegistrationDate { get; set; }

        // Navigation properties
        [ForeignKey("TournamentId")]
        public Tournament Tournament { get; set; }

        [ForeignKey("PlayerId")]
        public Player Player { get; set; }
    }
}