using Microsoft.EntityFrameworkCore;
using Tennis_Card_Game.Data;
using Tennis_Card_Game.Interfaces;
using Tennis_Card_Game.Models;
using Tennis_Card_Game.ViewModel;

namespace Tennis_Card_Game.Services
{
    public class TournamentService : ITournamentService
    {
        private readonly Tennis_Card_GameContext _context;
        private readonly ILogger<TournamentService> _logger;
        private readonly ITournamentMatchesGenerator _matchesGenerator;
        private readonly ITournamentEligibilityChecker _eligibilityChecker;

        public TournamentService(
            Tennis_Card_GameContext context,
            ILogger<TournamentService> logger,
            ITournamentMatchesGenerator matchesGenerator,
            ITournamentEligibilityChecker eligibilityChecker)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _matchesGenerator = matchesGenerator ?? throw new ArgumentNullException(nameof(matchesGenerator));
            _eligibilityChecker = eligibilityChecker ?? throw new ArgumentNullException(nameof(eligibilityChecker));
        }

        public async Task<List<TournamentViewModel>> GetAllTournamentsAsync() =>
            await _context.Tournaments
                .AsNoTracking()
                .Include(t => t.Surface)
                .Include(t => t.Matches)
                .OrderByDescending(t => t.StartTime)
                .Select(t => TournamentViewModelMapper.MapToViewModel(t))
                .ToListAsync();

        public async Task<TournamentViewModel?> GetTournamentDetailsAsync(int tournamentId)
        {
            var tournament = await _context.Tournaments
                .AsNoTracking()
                .Include(t => t.Surface)
                .Include(t => t.Matches)
                    .ThenInclude(m => m.Player1)
                .Include(t => t.Matches)
                    .ThenInclude(m => m.Player2)
                .FirstOrDefaultAsync(t => t.Id == tournamentId);

            if (tournament == null)
                return null;

            DateTime now = DateTime.Now;
            DateTime tournamentStartDateTime = DateTime.Today.Add(tournament.StartTime);

            var filteredMatches = now >= tournamentStartDateTime
                ? tournament.Matches.ToList()
                : new List<Match>();

            return TournamentViewModelMapper.MapToViewModel(new Tournament
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
                Matches = filteredMatches
            });
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
                DateTime endDateTime = t.EndTime.TotalHours == 0
                    ? DateTime.Today.AddDays(1)
                    : DateTime.Today.Add(t.EndTime);

                bool isActive = startDateTime <= now && now <= endDateTime;

                _logger.LogInformation(
                    "Tournament: {Name}, Start: {Start}, End: {End}, Current: {Now}, IsActive: {IsActive}",
                    t.Name, startDateTime, endDateTime, now, isActive);

                return isActive;
            }).ToList();

            _logger.LogInformation("Found {Count} current tournaments", currentTournaments.Count);
            return currentTournaments.Select(TournamentViewModelMapper.MapToViewModel).ToList();
        }

        public async Task<(bool isEligible, string errorMessage, Player? player, Tournament? tournament)>
            CheckTournamentEligibilityAsync(int tournamentId, string? userId)
        {
            return await _eligibilityChecker.CheckEligibilityAsync(tournamentId, userId);
        }

        public async Task<(bool Success, string ErrorMessage)> JoinTournamentAsync(int tournamentId, string userId)
        {
            try
            {
                var (isEligible, errorMessage, userPlayer, tournament) =
                    await CheckTournamentEligibilityAsync(tournamentId, userId);

                if (!isEligible || tournament == null || userPlayer == null)
                    return (false, errorMessage);

                Surface? surface = await _context.Surfaces.FindAsync(tournament.SurfaceId);
                if (surface == null)
                    return (false, "The playing surface was not found!");

                bool tournamentHasMatches = await _context.Matches
                    .AnyAsync(m => m.TournamentId == tournamentId);

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

                    if (!tournamentHasMatches)
                        await _matchesGenerator.GenerateTournamentMatchesAsync(tournament, userPlayer, surface);
                    else
                        await _matchesGenerator.AddPlayerToTournamentAsync(tournament, userPlayer, surface);

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

        
    }
}