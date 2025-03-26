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

namespace Tennis_Card_Game.Services
{
    public class MatchCompletionService : BackgroundService
    {
        private readonly IServiceProvider _services;
        private readonly TimeSpan _checkInterval = TimeSpan.FromMinutes(1);

        public MatchCompletionService(IServiceProvider services)
        {
            _services = services;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                await ProcessCompletedMatches();
                await Task.Delay(_checkInterval, stoppingToken);
            }
        }

        private async Task ProcessCompletedMatches()
        {
            using var scope = _services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<Tennis_Card_GameContext>();
            var tournamentMatchesGenerator = scope.ServiceProvider.GetRequiredService<ITournamentMatchesGenerator>();

            var bucharestTimeZone = TimeZoneInfo.FindSystemTimeZoneById("E. Europe Standard Time");
            var nowBucharest = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, bucharestTimeZone);

            var existingPlayerIds = await context.Players
                .AsNoTracking()
                .Select(p => p.Id)
                .ToHashSetAsync();

            if (existingPlayerIds.Count == 0) return;

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
                using var transaction = await context.Database.BeginTransactionAsync();
                try
                {
                    var matchToUpdate = await context.Matches
                        .Include(m => m.Sets)
                        .FirstOrDefaultAsync(m => m.Id == match.Id);

                    if (matchToUpdate == null) continue;

                    await GenerateMatchScore(matchToUpdate, context);
                    await transaction.CommitAsync();

                    if (matchToUpdate.Round < 4 && matchToUpdate.TournamentId.HasValue)
                    {
                        await tournamentMatchesGenerator.UpdateNextRoundMatchesAsync(matchToUpdate.TournamentId.Value);
                    }
                }
                catch
                {
                    await transaction.RollbackAsync();
                }
            }
        }

        private async Task GenerateMatchScore(Match match, Tennis_Card_GameContext context)
        {
            var player1 = await context.Players
                .Include(p => p.PlayingStyle)
                .Include(p => p.SpecialAbility)
                .FirstOrDefaultAsync(p => p.Id == match.Player1Id);

            var player2 = await context.Players
                .Include(p => p.PlayingStyle)
                .Include(p => p.SpecialAbility)
                .FirstOrDefaultAsync(p => p.Id == match.Player2Id);

            if (player1 == null || player2 == null) return;

            var (player1Strength, player2Strength) = CalculatePlayerStrengths(player1, player2);
            var player1WinProbability = CalculateWinProbability(player1Strength, player2Strength);

            var random = new Random();
            bool player1Wins = random.NextDouble() < player1WinProbability;

            var scores = GenerateRealisticScores(player1Wins, player1Strength, player2Strength);

            match.Player1Sets = scores[0];
            match.Player2Sets = scores[1];
            match.IsCompleted = true;
            match.EndTime ??= DateTime.UtcNow;

            context.Matches.Update(match);
            await context.SaveChangesAsync();

            await UpdatePlayersAfterMatch(player1, player2, player1Wins, context);
        }

        private static (double player1Strength, double player2Strength) CalculatePlayerStrengths(Player player1, Player player2)
        {
            double CalculateIndividualStrength(Player player)
            {
                double baseStrength = player.Level * 10.0;
                double styleOffensiveBonus = player.PlayingStyle?.OffensiveBonus * 0.5 ?? 0;
                double styleDefensiveBonus = player.PlayingStyle?.DefensiveBonus * 0.5 ?? 0;
                double abilityBonus = player.SpecialAbility?.EffectValue * 0.3 ?? 0;

                return baseStrength + styleOffensiveBonus + styleDefensiveBonus + abilityBonus;
            }

            return (
                CalculateIndividualStrength(player1),
                CalculateIndividualStrength(player2)
            );
        }

        private static double CalculateWinProbability(double player1Strength, double player2Strength) =>
            1 / (1 + Math.Pow(10, -(player1Strength - player2Strength) / 400));

        private static int[] GenerateRealisticScores(bool player1Wins, double player1Strength, double player2Strength)
        {
            var random = new Random();
            double strengthRatio = player1Wins
                ? player1Strength / (player1Strength + player2Strength)
                : player2Strength / (player1Strength + player2Strength);

            int winnerSets = 2;
            int loserSets = random.Next(strengthRatio > 0.7 ? 0 : 1);

            return player1Wins
                ? new[] { winnerSets, loserSets }
                : new[] { loserSets, winnerSets };
        }

        private async Task UpdatePlayersAfterMatch(Player player1, Player player2, bool player1Wins, Tennis_Card_GameContext context)
        {
            var (winner, loser) = player1Wins ? (player1, player2) : (player2, player1);

            winner.Experience += 15;
            winner.Level = CalculateLevel(winner.Experience);
            winner.Momentum = "Positive";
            winner.CurrentEnergy = Math.Min(winner.MaxEnergy, winner.CurrentEnergy +
                (winner.PlayingStyle?.EnergyEfficiency ?? 10));

            loser.Experience += 5;
            loser.Level = CalculateLevel(loser.Experience);
            loser.Momentum = "Negative";
            loser.CurrentEnergy = Math.Max(0, loser.CurrentEnergy -
                (loser.PlayingStyle?.EnergyEfficiency ?? 5));

            context.Players.UpdateRange(winner, loser);
            await context.SaveChangesAsync();
        }

        private static int CalculateLevel(int experience) =>
            1 + (int)Math.Floor(Math.Sqrt(experience / 100.0));
    }
}