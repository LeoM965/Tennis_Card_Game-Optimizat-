using Microsoft.EntityFrameworkCore;
using Tennis_Card_Game.Data;
using Tennis_Card_Game.Models;

namespace Tennis_Card_Game.Services
{
    public class PlayerService
    {
        private const int BASE_XP_PER_LEVEL = 1000;
        private const double XP_GROWTH_MULTIPLIER = 1.75;
        private const int BASE_SKILL_POINTS = 1;
        private const int SKILL_POINTS_GROWTH_RATE = 2;

        private readonly Tennis_Card_GameContext _context;

        public PlayerService(Tennis_Card_GameContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public async Task<int> EnsureValidPlayerId(int id)
        {
            if (id == 0)
            {
                Player? firstPlayer = await _context.Players
                    .OrderBy(p => p.Id)
                    .FirstOrDefaultAsync();

                if (firstPlayer == null)
                {
                    throw new InvalidOperationException("No players exist in the system");
                }

                return firstPlayer.Id;
            }
            return id;
        }

        public async Task PerformSkillUpgrade(Player player, string upgradeType, int upgradeId)
        {
            int skillPointsCost = CalculateSkillUpgradeCost(player);

            switch (upgradeType)
            {
                case "PlayingStyle":
                    PlayingStyle? newPlayingStyle = await _context.PlayingStyles.FindAsync(upgradeId);
                    if (newPlayingStyle != null)
                    {
                        player.PlayingStyle = newPlayingStyle;
                        player.PlayingStyleId = newPlayingStyle.Id;
                        player.Experience -= skillPointsCost;
                        player.MaxEnergy += 5;
                    }
                    break;

                case "SpecialAbility":
                    SpecialAbility? newSpecialAbility = await _context.SpecialAbilities.FindAsync(upgradeId);
                    if (newSpecialAbility != null)
                    {
                        player.SpecialAbility = newSpecialAbility;
                        player.SpecialAbilityId = newSpecialAbility.Id;
                        player.Experience -= skillPointsCost;
                        player.CurrentEnergy = player.MaxEnergy;
                    }
                    break;
            }
        }

        public int CalculateXpForNextLevel(int targetLevel)
        {
            return targetLevel == 1 ? 1000 : (int)(BASE_XP_PER_LEVEL * Math.Pow(XP_GROWTH_MULTIPLIER, targetLevel - 1));
        }

        public int CalculateXpToNextLevel(Player player)
        {
            if (player.Level == 1)
            {
                return 1000 - player.Experience;
            }

            int currentLevelXp = CalculateXpForNextLevel(player.Level);
            int nextLevelXp = CalculateXpForNextLevel(player.Level + 1);

            return nextLevelXp - player.Experience;
        }

        public int CalculateAvailableSkillPoints(Player player)
        {
            int basePoints = BASE_SKILL_POINTS + (player.Level / SKILL_POINTS_GROWTH_RATE);

            return player.PlayingStyle.Name switch
            {
                "Aggressive Baseliner" => basePoints + 1,
                "Serve and Volleyer" => basePoints + 1,
                "Defensive Baseliner" => basePoints,
                "Counter-Puncher" => basePoints + 1,
                "Big Server" => basePoints + 1,
                _ => basePoints
            };
        }

        public int CalculateSkillUpgradeCost(Player player)
        {
            int baseCost = 500;
            int levelMultiplier = player.Level * 100;

            return player.PlayingStyle.Name switch
            {
                "Aggressive Baseliner" => baseCost + levelMultiplier + 250,
                "Serve and Volleyer" => baseCost + levelMultiplier + 300,
                "Defensive Baseliner" => baseCost + levelMultiplier + 200,
                "Counter-Puncher" => baseCost + levelMultiplier + 150,
                "Big Server" => baseCost + levelMultiplier + 350,
                _ => baseCost + levelMultiplier
            };
        }

        public void LevelUpPlayer(Player player)
        {
            int currentLevelXp = CalculateXpForNextLevel(player.Level);

            if (player.Experience >= currentLevelXp)
            {
                player.Level++;

                int energyBonus = player.PlayingStyle.Name switch
                {
                    "Aggressive Baseliner" => 8,
                    "Serve and Volleyer" => 10,
                    "Defensive Baseliner" => 6,
                    "Counter-Puncher" => 7,
                    "Big Server" => 9,
                    _ => 5
                };

                player.MaxEnergy += energyBonus + (player.Level * 2);
                player.CurrentEnergy = player.MaxEnergy;

                if (player.Level == 5)
                {
                    player.Experience += 500;
                }

                if (new Random().Next(20) == 0)
                {
                    player.Experience += 250;
                }
            }
        }

        public double CalculateLevelProgressPercentage(Player player)
        {
            if (player.Level == 1)
            {
                return Math.Min(100, (player.Experience / 1000.0) * 100);
            }

            int currentLevelXp = CalculateXpForNextLevel(player.Level);
            int nextLevelXp = CalculateXpForNextLevel(player.Level + 1);

            return Math.Min(100, (double)(player.Experience - currentLevelXp) / (nextLevelXp - currentLevelXp) * 100);
        }

        
    }
}