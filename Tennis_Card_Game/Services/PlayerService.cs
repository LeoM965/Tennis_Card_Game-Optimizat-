using Microsoft.EntityFrameworkCore;
using Tennis_Card_Game.Data;
using Tennis_Card_Game.Models;

namespace Tennis_Card_Game.Services
{
    public class PlayerService
    {
        private const int BASE_XP_PER_LEVEL = 1000;
        private const double XP_GROWTH_MULTIPLIER = 2;
        private const int BASE_SKILL_POINTS = 1;
        private const int SKILL_POINTS_GROWTH_RATE = 2;

        private readonly Tennis_Card_GameContext _context;

        public PlayerService(Tennis_Card_GameContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public int CalculateXpForLevel(int level)
        {
            if (level <= 1) return 0;
            return (int)(BASE_XP_PER_LEVEL * Math.Pow(XP_GROWTH_MULTIPLIER, level - 2));
        }

        public void LevelUpPlayer(Player player)
        {
            int nextLevelXp = CalculateXpForLevel(player.Level + 1);

            if (player.Experience >= nextLevelXp)
            {
                player.Level++;
                player.Experience = 0;

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
            }
        }

        public int CalculateXpToNextLevel(Player player) =>
            CalculateXpForLevel(player.Level + 1);

        public double CalculateLevelProgressPercentage(Player player)
        {
            int currentLevelXp = CalculateXpForLevel(player.Level);
            int nextLevelXp = CalculateXpForLevel(player.Level + 1);

            return nextLevelXp > 0
                ? Math.Clamp(((double)player.Experience / nextLevelXp) * 100, 0, 100)
                : 0;
        }

        public int CalculateAvailableSkillPoints(Player player) =>
            BASE_SKILL_POINTS + (player.Level / SKILL_POINTS_GROWTH_RATE) +
            (player.PlayingStyle.Name is "Aggressive Baseliner" or "Serve and Volleyer" or "Counter-Puncher" or "Big Server" ? 1 : 0);

        public int CalculateSkillUpgradeCost(Player player) =>
            500 + (player.Level * 100) +
            player.PlayingStyle.Name switch
            {
                "Aggressive Baseliner" => 250,
                "Serve and Volleyer" => 300,
                "Defensive Baseliner" => 200,
                "Counter-Puncher" => 150,
                "Big Server" => 350,
                _ => 0
            };

        public async Task<int> EnsureValidPlayerId(int id) =>
            id == 0
                ? (await _context.Players.OrderBy(p => p.Id).FirstOrDefaultAsync())?.Id
                  ?? throw new InvalidOperationException("No players exist in the system")
                : id;

        public async Task<bool> PerformSkillUpgrade(Player player, string upgradeType, int upgradeId)
        {
            int skillPointsCost = CalculateSkillUpgradeCost(player);

            if (player.Experience < skillPointsCost) return false;

            switch (upgradeType)
            {
                case "PlayingStyle":
                    var newPlayingStyle = await _context.PlayingStyles.FindAsync(upgradeId);
                    if (newPlayingStyle != null)
                    {
                        player.PlayingStyle = newPlayingStyle;
                        player.PlayingStyleId = newPlayingStyle.Id;
                        player.Experience -= skillPointsCost;
                        player.MaxEnergy += 5;
                        await _context.SaveChangesAsync();
                        return true;
                    }
                    break;

                case "SpecialAbility":
                    var newSpecialAbility = await _context.SpecialAbilities.FindAsync(upgradeId);
                    if (newSpecialAbility != null)
                    {
                        player.SpecialAbility = newSpecialAbility;
                        player.SpecialAbilityId = newSpecialAbility.Id;
                        player.Experience -= skillPointsCost;
                        player.CurrentEnergy = player.MaxEnergy;
                        await _context.SaveChangesAsync();
                        return true;
                    }
                    break;
            }

            return false;
        }
    }
}