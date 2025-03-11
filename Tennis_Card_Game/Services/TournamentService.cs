using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Tennis_Card_Game.Data;
using Tennis_Card_Game.Models;
using Tennis_Card_Game.ViewModel;

namespace Tennis_Card_Game.Services
{
    public class TournamentService : ITournamentService
    {
        private readonly Tennis_Card_GameContext _context;
        private readonly ILogger<TournamentService> _logger;
        private readonly IMatchService _matchService;

        public TournamentService(
            Tennis_Card_GameContext context,
            ILogger<TournamentService> logger,
            IMatchService matchService)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _matchService = matchService ?? throw new ArgumentNullException(nameof(matchService));
        }

        public async Task<List<TournamentViewModel>> GetAllTournamentsAsync()
        {
            return await _context.Tournaments
                .AsNoTracking()
                .Include(t => t.Surface)
                .Include(t => t.Matches)
                .OrderByDescending(t => t.StartTime)
                .Select(t => MapToTournamentViewModel(t))
                .ToListAsync();
        }

        public async Task<TournamentViewModel?> GetTournamentDetailsAsync(int tournamentId)
        {
            var tournament = await _context.Tournaments
                .AsNoTracking()
                .Include(t => t.Surface)
                .Include(t => t.Matches)
                    .ThenInclude(m => m.Player1)
                .Include(t => t.Matches)
                    .ThenInclude(m => m.Player2)
                .Where(t => t.Id == tournamentId)
                .FirstOrDefaultAsync();

            if (tournament == null)
                return null;

            DateTime now = DateTime.Now;
            DateTime tournamentStartDateTime = DateTime.Today.Add(tournament.StartTime);

            var tournamentWithFilteredMatches = new Tournament
            {
                Id = tournament.Id,
                Name = tournament.Name,
                StartTime = tournament.StartTime,
                EndTime = tournament.EndTime,
                Surface = tournament.Surface,
                SurfaceId = tournament.SurfaceId,
                Level = tournament.Level,
                XpReward = tournament.XpReward,
                CoinReward = tournament.CoinReward,
                Matches = now >= tournamentStartDateTime ? tournament.Matches.ToList() : new List<Match>()
            };

            return MapToTournamentViewModel(tournamentWithFilteredMatches);
        }
        public async Task<List<TournamentViewModel>> GetCurrentTournamentsAsync()
        {
            DateTime now = DateTime.Now;

            _logger.LogInformation("Checking for current tournaments at {CurrentTime}", now);

            var allTournaments = await _context.Tournaments
                .AsNoTracking()
                .Include(t => t.Surface)
                .Include(t => t.Matches)
                    .ThenInclude(m => m.Player1)
                .Include(t => t.Matches)
                    .ThenInclude(m => m.Player2)
                .ToListAsync();

            var currentTournaments = allTournaments.Where(t =>
            {
                DateTime startDateTime = DateTime.Today.Add(t.StartTime);
                DateTime endDateTime;

                if (t.EndTime.TotalHours == 0)
                {
                    endDateTime = DateTime.Today.AddDays(1);
                }
                else
                {
                    endDateTime = DateTime.Today.Add(t.EndTime);
                }

                _logger.LogInformation(
                    "Tournament: {Name}, Start: {Start}, End: {End}, Current: {Now}, IsActive: {IsActive}",
                    t.Name,
                    startDateTime,
                    endDateTime,
                    now,
                    (startDateTime <= now && now <= endDateTime));

                return startDateTime <= now && now <= endDateTime;
            })
            .ToList();

            _logger.LogInformation("Found {Count} current tournaments", currentTournaments.Count);

            return currentTournaments.Select(t => MapToTournamentViewModel(t)).ToList();
        }

        public async Task<(bool isEligible, string errorMessage, Player? player, Tournament? tournament)> CheckTournamentEligibilityAsync(int tournamentId, string? userId)
        {
            if (string.IsNullOrEmpty(userId))
                return (false, "You must be logged in to join a tournament.", null, null);

            Tournament? tournament = await _context.Tournaments
                .AsNoTracking()
                .Include(t => t.Surface)
                .FirstOrDefaultAsync(t => t.Id == tournamentId);

            if (tournament == null)
                return (false, "Tournament not found.", null, null);

            Player? userPlayer = await _context.Players
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.UserId == userId);

            if (userPlayer == null)
                return (false, "You don't have a player to participate in the tournament!", null, tournament);

            DateTime currentTime = DateTime.Now;
            DateTime tournamentStartDateTime = DateTime.Today.Add(tournament.StartTime);
            DateTime registrationOpenTime = tournamentStartDateTime.AddHours(-1);

            if (currentTime < registrationOpenTime)
                return (false, "Registration is not open yet. You can register 1 hour before the tournament starts.", userPlayer, tournament);

            DateTime tournamentEndDateTime = tournament.EndTime.TotalHours == 0
                ? DateTime.Today.AddDays(1)
                : DateTime.Today.Add(tournament.EndTime);

            DateTime registrationCloseTime = tournamentEndDateTime.AddMinutes(-5);

            if (currentTime > registrationCloseTime)
                return (false, "Registration is closed. The tournament is about to end.", userPlayer, tournament);

            bool alreadyParticipating = await _context.Matches
                .AsNoTracking()
                .AnyAsync(m => m.TournamentId == tournamentId &&
                          (m.Player1Id == userPlayer.Id || m.Player2Id == userPlayer.Id));

            if (alreadyParticipating)
                return (false, "You are already participating in this tournament!", userPlayer, tournament);

            bool alreadyRegistered = await _context.TournamentRegistrations
                .AsNoTracking()
                .AnyAsync(r => r.TournamentId == tournamentId && r.PlayerId == userPlayer.Id);

            if (alreadyRegistered)
                return (false, "You are already registered in this tournament!", userPlayer, tournament);

            return (true, string.Empty, userPlayer, tournament);
        }

        public async Task<(bool Success, string ErrorMessage)> JoinTournamentAsync(int tournamentId, string userId)
        {
            try
            {
                var (isEligible, errorMessage, userPlayer, tournament) =
                    await CheckTournamentEligibilityAsync(tournamentId, userId);

                if (!isEligible || tournament == null || userPlayer == null)
                {
                    return (false, errorMessage);
                }

                Surface? surface = await _context.Surfaces.FindAsync(tournament.SurfaceId);
                if (surface == null)
                {
                    return (false, "The playing surface was not found!");
                }

                List<Player> aiOpponents = await _context.Players
                    .Where(p => p.UserId == null)
                    .Include(p => p.PlayingStyle)
                    .OrderBy(p => Guid.NewGuid())
                    .Take(15)
                    .ToListAsync();

                if (aiOpponents.Count < 15)
                {
                    return (false, "There are not enough AI players in the database to generate a tournament!");
                }

                using var transaction = await _context.Database.BeginTransactionAsync();

                try
                {
                    TournamentRegistration registration = new TournamentRegistration
                    {
                        TournamentId = tournamentId,
                        PlayerId = userPlayer.Id,
                        RegistrationDate = DateTime.Now
                    };

                    _context.TournamentRegistrations.Add(registration);
                    await _context.SaveChangesAsync();

                    await GenerateTournamentMatchesAsync(tournament, userPlayer, aiOpponents, surface);

                    await transaction.CommitAsync();
                    return (true, string.Empty);
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    throw new Exception($"Failed to complete tournament registration: {ex.Message}", ex);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during tournament join for tournament {TournamentId} by user {UserId}",
                    tournamentId, userId);
                return (false, $"Error joining tournament: {ex.Message}");
            }
        }

        private async Task GenerateTournamentMatchesAsync(Tournament tournament, Player userPlayer, List<Player> aiOpponents, Surface surface)
        {
            if (aiOpponents.Count < 15)
                throw new InvalidOperationException("Not enough AI players for tournament");

            Player? tbdPlayer = await _context.Players
                .FirstOrDefaultAsync(p => p.Name == "Player");

            if (tbdPlayer == null)
            {
                tbdPlayer = new Player
                {
                    Name = "PlayerTBD",
                    PlayingStyleId = aiOpponents.First().PlayingStyleId,
                    UserId = null
                };
                _context.Players.Add(tbdPlayer);
                await _context.SaveChangesAsync();
            }

            var matches = new List<Match>();

            DateTime startDateTime = DateTime.Today.Add(tournament.StartTime);
            DateTime endDateTime = tournament.EndTime.TotalHours == 0
                ? DateTime.Today.AddDays(1)
                : DateTime.Today.Add(tournament.EndTime);
            TimeSpan tournamentDuration = endDateTime - startDateTime;

            matches.Add(CreateMatch(tournament.Id, userPlayer.Id, aiOpponents[0].Id, surface.Id, 1, 1));

            for (int i = 1; i < 8; i++)
            {
                matches.Add(CreateMatch(tournament.Id, aiOpponents[i * 2 - 1].Id, aiOpponents[i * 2].Id, surface.Id, 1, i + 1));
            }

            for (int i = 0; i < 4; i++)
            {
                matches.Add(CreateMatch(tournament.Id, tbdPlayer.Id, tbdPlayer.Id, surface.Id, 2, i + 1));
            }

            for (int i = 0; i < 2; i++)
            {
                matches.Add(CreateMatch(tournament.Id, tbdPlayer.Id, tbdPlayer.Id, surface.Id, 3, i + 1));
            }

            matches.Add(CreateMatch(tournament.Id, tbdPlayer.Id, tbdPlayer.Id, surface.Id, 4, 1));

            int totalMatches = matches.Count;
            TimeSpan interval = new TimeSpan(tournamentDuration.Ticks / (totalMatches + 1));

            for (int i = 0; i < totalMatches; i++)
            {
                matches[i].StartTime = startDateTime.Add(interval * (i + 1));
            }

            HashSet<int> playerIds = matches
                .SelectMany(m => new[] { m.Player1Id, m.Player2Id })
                .Where(id => id != tbdPlayer.Id)
                .ToHashSet();

            int existingPlayerCount = await _context.Players
                .Where(p => playerIds.Contains(p.Id))
                .CountAsync();

            if (existingPlayerCount != playerIds.Count)
                throw new InvalidOperationException("One or more players not found in database");

            await _context.Matches.AddRangeAsync(matches);
            await _context.SaveChangesAsync();
        }

        private Match CreateMatch(int tournamentId, int player1Id, int player2Id, int surfaceId, int round, int matchOrder)
        {
            return new Match
            {
                TournamentId = tournamentId,
                Player1Id = player1Id,
                Player2Id = player2Id,
                SurfaceId = surfaceId,
                Round = round,
                MatchOrder = matchOrder,
                IsCompleted = false,
                Player1Sets = 0,
                Player2Sets = 0
            };
        }

        public async Task EnsureMatchesCompletedAsync(int tournamentId)
        {
            Tournament? tournament = await _context.Tournaments
                .Include(t => t.Matches)
                    .ThenInclude(m => m.Player1)
                .Include(t => t.Matches)
                    .ThenInclude(m => m.Player2)
                .FirstOrDefaultAsync(t => t.Id == tournamentId);

            if (tournament == null)
                return;

            DateTime currentTime = DateTime.Now;
            DateTime tournamentEndDateTime = DateTime.Today.Add(tournament.EndTime);

            if (currentTime >= tournamentEndDateTime.AddMinutes(-10))
            {
                List<Match> incompleteMatches = tournament.Matches
                    .Where(m => !m.IsCompleted)
                    .OrderBy(m => m.Round)
                    .ThenBy(m => m.MatchOrder)
                    .ToList();

                if (incompleteMatches.Count == 0)
                    return;

                var random = new Random();

                foreach (Match match in incompleteMatches)
                {
                    bool player1Wins = random.Next(2) == 0;

                    if (player1Wins)
                    {
                        match.Player1Sets = random.Next(2, 4);
                        match.Player2Sets = random.Next(0, match.Player1Sets);
                    }
                    else
                    {
                        match.Player2Sets = random.Next(2, 4);
                        match.Player1Sets = random.Next(0, match.Player2Sets);
                    }

                    match.IsCompleted = true;
                    match.EndTime = currentTime;

                    await _matchService.UpdateNextRoundMatchAsync(match);
                }

                await _context.SaveChangesAsync();
                _logger.LogInformation("Auto-completed {MatchCount} matches for tournament {TournamentId}",
                    incompleteMatches.Count, tournamentId);
            }
        }

        private static TournamentViewModel MapToTournamentViewModel(Tournament tournament)
        {
            return new TournamentViewModel
            {
                Id = tournament.Id,
                Name = tournament.Name,
                StartTime = tournament.StartTime,
                EndTime = tournament.EndTime,
                Surface = tournament.Surface.Name,
                Level = tournament.Level,
                XpReward = tournament.XpReward,
                CoinReward = tournament.CoinReward,
                MatchCount = tournament.Matches.Count,
                Matches = tournament.Matches.Select(m => new MatchViewModel
                {
                    Id = m.Id,
                    Player1Name = m.Player1?.Name ?? "Unknown",
                    Player2Name = m.Player2?.Name ?? "Unknown",
                    Player1Sets = m.Player1Sets,
                    Player2Sets = m.Player2Sets,
                    IsCompleted = m.IsCompleted,
                    StartTime = m.StartTime
                }).ToList()
            };
        }
    }
}