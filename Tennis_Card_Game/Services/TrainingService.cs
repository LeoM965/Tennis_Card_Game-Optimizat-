using Tennis_Card_Game.Data;
using Tennis_Card_Game.Models;

namespace Tennis_Card_Game.Services
{
    public class TrainingService
    {
        private readonly Tennis_Card_GameContext _context;

        public TrainingService(Tennis_Card_GameContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public List<TrainingModule> GenerateTrainingModules(Player player) =>
            new()
            {
                new TrainingModule
                {
                    Name = "Physical Conditioning",
                    Description = "Improve overall fitness and energy management",
                    ExperienceReward = 60,
                    EnergyRequired = 20
                },
                new TrainingModule
                {
                    Name = "Technical Skills",
                    Description = "Enhance precision and shot techniques",
                    ExperienceReward = 80,
                    EnergyRequired = 30
                },
                new TrainingModule
                {
                    Name = "Mental Training",
                    Description = "Develop focus and resilience",
                    ExperienceReward = 120,
                    EnergyRequired = 40
                }
            };

        public List<TrainingModule> GenerateRecommendedTrainingModules(Player player) =>
            GenerateTrainingModules(player)
                .Where(tm => tm.EnergyRequired <= player.CurrentEnergy)
                .ToList();

        public int CalculateTrainingXp(Player player, int baseXp)
        {
            double playingStyleBonus = player.PlayingStyle.Name switch
            {
                "Aggressive Baseliner" => 0.2,
                "Serve and Volleyer" => 0.15,
                "Defensive Baseliner" => 0.1,
                "Counter-Puncher" => 0.25,
                "Big Server" => 0.18,
                _ => 0.12
            };

            double specialAbilityBonus = player.SpecialAbility != null
                ? player.SpecialAbility.Name switch
                {
                    "Second Wind" => 0.1,
                    "Focus Mode" => 0.15,
                    "Power Surge" => 0.2,
                    "Card Draw" => 0.12,
                    _ => 0.05
                }
                : 0.05;

            double totalBonus = playingStyleBonus + specialAbilityBonus;
            return (int)(baseXp * (1 + totalBonus));
        }

        public double CalculateBonusMultiplier(Player player)
        {
            double playingStyleBonus = player.PlayingStyle.Name switch
            {
                "Aggre[dbo].[TournamentRegistrations]ssive Baseliner" => 0.2,
                "Serve and Volleyer" => 0.15,
                "Defensive Baseliner" => 0.1,
                "Counter-Puncher" => 0.25,
                "Big Server" => 0.18,
                _ => 0.12
            };

            double specialAbilityBonus = player.SpecialAbility != null
                ? player.SpecialAbility.Name switch
                {
                    "Second Wind" => 0.1,
                    "Focus Mode" => 0.15,
                    "Power Surge" => 0.2,
                    "Card Draw" => 0.12,
                    _ => 0.05
                }
                : 0.05;

            return playingStyleBonus + specialAbilityBonus;
        }
    }
}