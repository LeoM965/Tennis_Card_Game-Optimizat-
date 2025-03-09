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
                    ExperienceReward = 50 + (player.Level * 10),
                    EnergyRequired = 20
                },
                new TrainingModule
                {
                    Name = "Technical Skills",
                    Description = "Enhance precision and shot techniques",
                    ExperienceReward = 75 + (player.Level * 15),
                    EnergyRequired = 30
                },
                new TrainingModule
                {
                    Name = "Mental Training",
                    Description = "Develop focus and resilience",
                    ExperienceReward = 100 + (player.Level * 20),
                    EnergyRequired = 40
                }
            };

        public List<TrainingModule> GenerateRecommendedTrainingModules(Player player) =>
            GenerateTrainingModules(player)
                .Where(tm => tm.EnergyRequired <= player.CurrentEnergy)
                .ToList();
    }
}