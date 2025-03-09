using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Tennis_Card_Game.Models
{
    public class Tournament
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; }

        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }

        [Required]
        [StringLength(50)]
        public string Level { get; set; } // "Regular", "Masters", "Grand Slam"

        public int XpReward { get; set; }
        public int CoinReward { get; set; }

        [ForeignKey("Surface")]
        public int SurfaceId { get; set; }
        public virtual Surface Surface { get; set; }

        public virtual ICollection<Match> Matches { get; set; }
        public Tournament()
        {
            Matches = new HashSet<Match>();
            XpReward = 0;
            CoinReward = 0;
            Name = string.Empty;
            Level = string.Empty;
            Surface = new Surface();
        }

        public Tournament(string name, TimeSpan startTime, Surface surface, string level, int xpReward, int coinReward)
            : this() 
        {
            Name = name;
            StartTime = startTime;
            Surface = surface;
            SurfaceId = surface.Id;
            Level = level;
            XpReward = xpReward;
            CoinReward = coinReward;

            double durationInHours = GetDurationInHours(Level);
            EndTime = StartTime.Add(TimeSpan.FromHours(durationInHours));
        }

        private double GetDurationInHours(string level)
        {
            return level switch
            {
                "Grand Slam" => 2.0, 
                "Masters" => 1.5,   
                "Regular" => 1.0,   
                _ => 1.0
            };
        }
    }
}