using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
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
        private readonly ILogger<MatchCompletionService> _logger;
        private readonly TimeSpan _checkInterval = TimeSpan.FromMinutes(1);

        public MatchCompletionService(IServiceProvider services, ILogger<MatchCompletionService> logger)
        {
            _services = services;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await ProcessCompletedMatches();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred while processing completed matches.");
                }

                await Task.Delay(_checkInterval, stoppingToken);
            }
        }

        private async Task ProcessCompletedMatches()
        {
            using var scope = _services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<Tennis_Card_GameContext>();
            var tournamentMatchesGenerator = scope.ServiceProvider.GetRequiredService<ITournamentMatchesGenerator>();

            TimeZoneInfo bucharestTimeZone = TimeZoneInfo.FindSystemTimeZoneById("E. Europe Standard Time");
            DateTime nowBucharest = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, bucharestTimeZone);

            if (!await context.Matches.AnyAsync())
            {
                _logger.LogInformation("No matches found in the database.");
                return;
            }

            var existingPlayerIds = await context.Players
                .AsNoTracking()
                .Select(p => p.Id)
                .ToHashSetAsync();

            if (existingPlayerIds.Count == 0)
            {
                _logger.LogWarning("No players available in the database.");
                return;
            }

            var completedMatches = await context.Matches
                .AsNoTracking()
                .Where(m =>
                    m.EndTime.HasValue &&
                    m.EndTime <= nowBucharest &&
                    !m.IsCompleted &&
                    existingPlayerIds.Contains(m.Player1Id) &&
                    existingPlayerIds.Contains(m.Player2Id))
                .ToListAsync();

            _logger.LogInformation("Found {Count} matches that need to be processed.", completedMatches.Count);

            foreach (var match in completedMatches)
            {
                using var transaction = await context.Database.BeginTransactionAsync();
                try
                {
                    var matchToUpdate = await context.Matches
                        .Include(m => m.Sets)
                        .FirstOrDefaultAsync(m => m.Id == match.Id);

                    if (matchToUpdate == null)
                    {
                        continue;
                    }

                    await GenerateMatchScore(matchToUpdate, context);
                    await transaction.CommitAsync();
                    _logger.LogInformation("Successfully processed match ID: {MatchId}.", match.Id);

                    if (matchToUpdate.Round < 4)
                    {
                        if (matchToUpdate.TournamentId.HasValue)
                        {
                            await tournamentMatchesGenerator.UpdateNextRoundMatchesAsync(matchToUpdate.TournamentId.Value);
                            _logger.LogInformation("Updated next round matches for tournament ID: {TournamentId}.", matchToUpdate.TournamentId.Value);
                        }
                        else
                        {
                            _logger.LogError("TournamentId is null for match ID: {MatchId}. Skipping UpdateNextRoundMatchesAsync.", matchToUpdate.Id);
                        }
                    }
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    _logger.LogError(ex, "Error processing match ID: {MatchId}.", match.Id);
                }
            }
        }

        private async Task GenerateMatchScore(Match match, Tennis_Card_GameContext context)
        {
            _logger.LogInformation("Generating score for match ID: {MatchId}.", match.Id);

            var random = new Random();
            bool player1Wins = random.Next(2) == 0;

            int[] scores = player1Wins ? new[] { 2, 0 } : new[] { 0, 2 };
            if (random.Next(2) == 0 && !player1Wins)
            {
                scores = new[] { 1, 2 };
            }

            match.Player1Sets = scores[0];
            match.Player2Sets = scores[1];
            match.IsCompleted = true;

            if (!match.EndTime.HasValue)
            {
                match.EndTime = DateTime.UtcNow;
            }

            context.Matches.Update(match);
            await context.SaveChangesAsync();

            _logger.LogInformation(
                "Finalized match ID: {MatchId}: P1 Sets = {P1Sets}, P2 Sets = {P2Sets}, EndTime = {EndTime}",
                match.Id, match.Player1Sets, match.Player2Sets, match.EndTime);
        }
    }
}