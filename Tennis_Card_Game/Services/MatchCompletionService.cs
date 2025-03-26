using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tennis_Card_Game.Data;
using Tennis_Card_Game.Interfaces;
using Tennis_Card_Game.Models;
using Tennis_Card_Game.Services;

namespace Tennis_Card_Game.Services
{
    public class MatchCompletionService : BackgroundService
    {
        private readonly IServiceProvider _services;
        private readonly TimeSpan _checkInterval = TimeSpan.FromMinutes(1);

        public MatchCompletionService(IServiceProvider services)
        {
            _services = services ?? throw new ArgumentNullException(nameof(services));
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                using (var scope = _services.CreateScope())
                {
                    var context = scope.ServiceProvider.GetRequiredService<Tennis_Card_GameContext>();
                    var tournamentMatchesGenerator = scope.ServiceProvider.GetRequiredService<ITournamentMatchesGenerator>();
                    var playerService = scope.ServiceProvider.GetRequiredService<PlayerService>();

                    await ProcessCompletedMatches(context, tournamentMatchesGenerator, playerService);
                }

                await Task.Delay(_checkInterval, stoppingToken);
            }
        }

        private async Task ProcessCompletedMatches(Tennis_Card_GameContext context, ITournamentMatchesGenerator tournamentMatchesGenerator, PlayerService playerService)
        {
            var bucharestTimeZone = TimeZoneInfo.FindSystemTimeZoneById("E. Europe Standard Time");
            var nowBucharest = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, bucharestTimeZone);

            var existingPlayerIds = await context.Players
                .AsNoTracking()
                .Select(p => p.Id)
                .ToHashSetAsync();

            if (!existingPlayerIds.Any()) return;

            var completedMatches = await context.Matches
                .AsNoTracking()
                .Where(m =>
                    m.EndTime.HasValue &&
                    m.EndTime <= nowBucharest &&
                    !m.IsCompleted &&
                    existingPlayerIds.Contains(m.Player1Id) &&
                    existingPlayerIds.Contains(m.Player2Id))
                .ToListAsync();

            foreach (var match in completedMatches)
            {
                try
                {
                    await GenerateMatchScore(match, context, playerService);

                    if (match.Round < 4 && match.TournamentId.HasValue)
                    {
                        await tournamentMatchesGenerator.UpdateNextRoundMatchesAsync(match.TournamentId.Value);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error processing match: {ex.Message}");
                }
            }
        }

        private async Task GenerateMatchScore(Match match, Tennis_Card_GameContext context, PlayerService playerService)
        {
            var players = await context.Players
                .Include(p => p.PlayingStyle)
                .Include(p => p.SpecialAbility)
                .Where(p => p.Id == match.Player1Id || p.Id == match.Player2Id)
                .ToDictionaryAsync(p => p.Id);

            if (!players.ContainsKey(match.Player1Id) || !players.ContainsKey(match.Player2Id)) return;

            var player1 = players[match.Player1Id];
            var player2 = players[match.Player2Id];

            var (player1Strength, player2Strength) = CalculatePlayerStrengths(player1, player2);
            var player1WinProbability = 1 / (1 + Math.Pow(10, -(player1Strength - player2Strength) / 400));

            var random = new Random();
            bool player1Wins = random.NextDouble() < player1WinProbability;

            var scores = GenerateRealisticScores(player1Wins, player1Strength, player2Strength);

            match.Player1Sets = scores[0];
            match.Player2Sets = scores[1];
            match.IsCompleted = true;
            match.EndTime ??= DateTime.UtcNow;

            context.Matches.Update(match);
            await context.SaveChangesAsync();

            await UpdatePlayersAfterMatch(player1, player2, player1Wins, context, playerService);
        }

        private static (double player1Strength, double player2Strength) CalculatePlayerStrengths(Player player1, Player player2)
        {
            static double CalculateIndividualStrength(Player player) =>
                player.Level * 10.0 +
                (player.PlayingStyle?.OffensiveBonus * 0.5 ?? 0) +
                (player.PlayingStyle?.DefensiveBonus * 0.5 ?? 0) +
                (player.SpecialAbility?.EffectValue * 0.3 ?? 0);

            return (CalculateIndividualStrength(player1), CalculateIndividualStrength(player2));
        }

        private static int[] GenerateRealisticScores(bool player1Wins, double player1Strength, double player2Strength)
        {
            var random = new Random();
            double strengthDifferenceRatio = player1Wins
                ? player1Strength / (player1Strength + player2Strength)
                : player2Strength / (player1Strength + player2Strength);

            var scoreOptions = new[]
            {
                new[] { 2, 0 },
                new[] { 2, 1 }
            };

            var weightedScores = strengthDifferenceRatio > 0.6
                ? Enumerable.Repeat(new[] { 2, 0 }, 3).Concat(Enumerable.Repeat(new[] { 2, 1 }, 1))
                : strengthDifferenceRatio > 0.55
                ? Enumerable.Repeat(new[] { 2, 0 }, 2).Concat(Enumerable.Repeat(new[] { 2, 1 }, 2))
                : Enumerable.Repeat(new[] { 2, 0 }, 1).Concat(Enumerable.Repeat(new[] { 2, 1 }, 3));

            var selectedScore = weightedScores.ElementAt(random.Next(weightedScores.Count()));
            return player1Wins ? selectedScore : new[] { selectedScore[1], selectedScore[0] };
        }

        private async Task UpdatePlayersAfterMatch(Player player1, Player player2, bool player1Wins, Tennis_Card_GameContext context, PlayerService playerService)
        {
            var (winner, loser) = player1Wins ? (player1, player2) : (player2, player1);

            int experienceGain = CalculateExperienceGain(winner, loser);
            winner.Experience = Math.Max(0, winner.Experience + experienceGain);
            loser.Experience = Math.Max(0, loser.Experience + Math.Max(20, experienceGain / 3));

            playerService.LevelUpPlayer(winner);
            playerService.LevelUpPlayer(loser);

            winner.Momentum = "Positive";
            winner.CurrentEnergy = Math.Min(winner.MaxEnergy, winner.CurrentEnergy + (winner.PlayingStyle?.EnergyEfficiency ?? 10));

            loser.Momentum = "Negative";
            loser.CurrentEnergy = Math.Max(0, loser.CurrentEnergy - (loser.PlayingStyle?.EnergyEfficiency ?? 5));

            context.Players.UpdateRange(winner, loser);
            await context.SaveChangesAsync();
        }

        private int CalculateExperienceGain(Player winner, Player loser)
        {
            int levelDifference = Math.Abs(winner.Level - loser.Level);
            double baseExperience = 100.0;

            if (levelDifference > 2) baseExperience *= 0.5;
            if (loser.Level > winner.Level) baseExperience *= 1.5;

            return (int)baseExperience;
        }
    }
}