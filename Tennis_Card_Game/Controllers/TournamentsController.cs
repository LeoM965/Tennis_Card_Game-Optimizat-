using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Tennis_Card_Game.Data;
using Tennis_Card_Game.Models;
using Tennis_Card_Game.ViewModel;

namespace Tennis_Card_Game.Controllers
{
    public class TournamentsController : Controller
    {
        private readonly Tennis_Card_GameContext _context;
        private readonly ILogger<TournamentsController> _logger;

        public TournamentsController(Tennis_Card_GameContext context, ILogger<TournamentsController> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<IActionResult> Index()
        {
            try
            {
                List<TournamentViewModel> tournaments = await _context.Tournaments
                    .Include(t => t.Surface)
                    .Include(t => t.Matches)
                    .OrderByDescending(t => t.StartTime)
                    .Select(t => new TournamentViewModel
                    {
                        Id = t.Id,
                        Name = t.Name,
                        StartTime = t.StartTime,
                        EndTime = t.EndTime,
                        Surface = t.Surface.Name,
                        Level = t.Level,
                        XpReward = t.XpReward,
                        CoinReward = t.CoinReward,
                        MatchCount = t.Matches.Count,
                        Matches = t.Matches.Select(m => new MatchViewModel
                        {
                            Id = m.Id,
                            Player1Name = m.Player1.Name,
                            Player2Name = m.Player2.Name,
                            Player1Sets = m.Player1Sets,
                            Player2Sets = m.Player2Sets,
                            IsCompleted = m.IsCompleted,
                            StartTime = m.StartTime
                        }).ToList()
                    })
                    .ToListAsync();
                return View(tournaments);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading tournaments");
                TempData["Error"] = "An error occurred while loading tournaments.";
                return View(new List<TournamentViewModel>());
            }
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
                return NotFound();

            try
            {
                await EnsureMatchesCompleted(id.Value);

                TournamentViewModel? tournament = await _context.Tournaments
                    .Include(t => t.Surface)
                    .Include(t => t.Matches)
                        .ThenInclude(m => m.Player1)
                    .Include(t => t.Matches)
                        .ThenInclude(m => m.Player2)
                    .Where(t => t.Id == id)
                    .Select(t => new TournamentViewModel
                    {
                        Id = t.Id,
                        Name = t.Name,
                        StartTime = t.StartTime,
                        EndTime = t.EndTime,
                        Surface = t.Surface.Name,
                        Level = t.Level,
                        XpReward = t.XpReward,
                        CoinReward = t.CoinReward,
                        MatchCount = t.Matches.Count,
                        Matches = t.Matches.Select(m => new MatchViewModel
                        {
                            Id = m.Id,
                            Player1Name = m.Player1.Name,
                            Player2Name = m.Player2.Name,
                            Player1Sets = m.Player1Sets,
                            Player2Sets = m.Player2Sets,
                            IsCompleted = m.IsCompleted,
                            StartTime = m.StartTime
                        }).ToList()
                    })
                    .FirstOrDefaultAsync();

                if (tournament == null)
                    return NotFound();

                return View(tournament);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading tournament details");
                TempData["Error"] = "An error occurred while loading tournament details.";
                return RedirectToAction(nameof(Index));
            }
        }

        public async Task<IActionResult> Current()
        {
            TimeSpan currentTime = DateTime.Now.TimeOfDay;
            DateTime today = DateTime.Today;

            List<TournamentViewModel> currentTournaments = await _context.Tournaments
                .Include(t => t.Surface)
                .Include(t => t.Matches)
                    .ThenInclude(m => m.Player1)
                .Include(t => t.Matches)
                    .ThenInclude(m => m.Player2)
                .Where(t => t.StartTime <= currentTime && t.EndTime >= currentTime)
                .Select(t => new TournamentViewModel
                {
                    Id = t.Id,
                    Name = t.Name,
                    StartTime = t.StartTime,
                    EndTime = t.EndTime,
                    Surface = t.Surface.Name,
                    Level = t.Level,
                    XpReward = t.XpReward,
                    CoinReward = t.CoinReward,
                    MatchCount = t.Matches.Count,
                    Matches = t.Matches.Select(m => new MatchViewModel
                    {
                        Id = m.Id,
                        Player1Name = m.Player1.Name,
                        Player2Name = m.Player2.Name,
                        Player1Sets = m.Player1Sets,
                        Player2Sets = m.Player2Sets,
                        IsCompleted = m.IsCompleted,
                        StartTime = m.StartTime
                    }).ToList()
                })
                .ToListAsync();

            return View(currentTournaments);
        }


        [HttpGet]
        public async Task<IActionResult> Join(int? id)
        {
            if (id == null)
                return NotFound();

            try
            {
                string? userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (userId == null)
                {
                    TempData["Error"] = "You must be logged in to join a tournament.";
                    return RedirectToAction(nameof(Details), new { id });
                }

                (bool isEligible, string errorMessage, Player? player, Tournament? tournament) = await CheckTournamentEligibility(id.Value, userId);
                if (!isEligible || tournament == null)
                {
                    TempData["Error"] = errorMessage;
                    return RedirectToAction(nameof(Details), new { id });
                }

                TournamentJoinViewModel viewModel = new TournamentJoinViewModel
                {
                    TournamentId = tournament.Id,
                    TournamentName = tournament.Name,
                    Surface = tournament.Surface.Name,
                    StartTime = tournament.StartTime,
                    Level = tournament.Level,
                    XpReward = tournament.XpReward,
                    CoinReward = tournament.CoinReward
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Join action");
                TempData["Error"] = "An error occurred while trying to join the tournament.";
                return RedirectToAction(nameof(Details), new { id });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> JoinConfirm(int tournamentId)
        {
            try
            {
                string? userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (userId == null)
                {
                    TempData["Error"] = "You must be logged in to join a tournament.";
                    return RedirectToAction(nameof(Details), new { id = tournamentId });
                }

                (bool isEligible, string errorMessage, Player? userPlayer, Tournament? tournament) =
                    await CheckTournamentEligibility(tournamentId, userId);

                if (!isEligible || tournament == null || userPlayer == null)
                {
                    TempData["Error"] = errorMessage;
                    return RedirectToAction(nameof(Details), new { id = tournamentId });
                }

                Surface? surface = await _context.Surfaces.FindAsync(tournament.SurfaceId);
                if (surface == null)
                {
                    TempData["Error"] = "The playing surface was not found!";
                    return RedirectToAction(nameof(Details), new { id = tournamentId });
                }

                List<Player> aiOpponents = await _context.Players
                    .Where(p => p.UserId == null)
                    .Join(_context.PlayingStyles,
                        player => player.PlayingStyleId,
                        style => style.Id,
                        (player, style) => player)
                    .OrderBy(p => Guid.NewGuid())
                    .Take(15)
                    .ToListAsync();

                if (aiOpponents.Count < 15)
                {
                    TempData["Error"] = "There are not enough AI players in the database to generate a tournament!";
                    return RedirectToAction(nameof(Details), new { id = tournamentId });
                }

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
                    await GenerateTournamentMatches(tournament, userPlayer, aiOpponents, surface);
                    TempData["Success"] = "You have successfully registered for the tournament and matches have been generated!";
                    return RedirectToAction(nameof(Details), new { id = tournamentId });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error during tournament join");
                    TempData["Error"] = $"Error joining tournament: {ex.Message}";
                    return RedirectToAction(nameof(Details), new { id = tournamentId });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in JoinConfirm");
                TempData["Error"] = $"An unexpected error occurred: {ex.Message}";
                return RedirectToAction(nameof(Details), new { id = tournamentId });
            }
        }

        private async Task<(bool isEligible, string errorMessage, Player? player, Tournament? tournament)> CheckTournamentEligibility(int tournamentId, string? userId)
        {
            if (string.IsNullOrEmpty(userId))
                return (false, "You must be logged in to join a tournament.", null, null);

            Tournament? tournament = await _context.Tournaments
                .Include(t => t.Surface)
                .FirstOrDefaultAsync(t => t.Id == tournamentId);

            if (tournament == null)
                return (false, "Tournament not found.", null, null);

            DateTime currentTime = DateTime.Now;
            DateTime tournamentStartDateTime = DateTime.Today.Add(tournament.StartTime);
            DateTime registrationOpenTime = tournamentStartDateTime.AddHours(-1);

            if (currentTime < registrationOpenTime)
                return (false, "Registration is not open yet. You can register 1 hour before the tournament starts.", null, tournament);

            DateTime tournamentEndDateTime = tournament.EndTime.TotalHours == 0
                ? DateTime.Today.AddDays(1)
                : DateTime.Today.Add(tournament.EndTime);

            DateTime registrationCloseTime = tournamentEndDateTime.AddMinutes(-5);

            if (currentTime > registrationCloseTime)
                return (false, "Registration is closed. The tournament is about to end.", null, tournament);

            Player? userPlayer = await _context.Players.FirstOrDefaultAsync(p => p.UserId == userId);
            if (userPlayer == null)
                return (false, "You don't have a player to participate in the tournament!", null, tournament);

            bool existingMatch = await _context.Matches
                .AnyAsync(m => m.TournamentId == tournamentId &&
                          (m.Player1Id == userPlayer.Id || m.Player2Id == userPlayer.Id));

            if (existingMatch)
                return (false, "You are already participating in this tournament!", null, tournament);

            bool existingRegistration = await _context.TournamentRegistrations
                .AnyAsync(r => r.TournamentId == tournamentId && r.PlayerId == userPlayer.Id);

            if (existingRegistration)
                return (false, "You are already registered in this tournament!", null, tournament);

            return (true, string.Empty, userPlayer, tournament);
        }

        private async Task GenerateTournamentMatches(Tournament tournament, Player userPlayer, List<Player> aiOpponents, Surface surface)
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

            List<Match> matches = new List<Match>();

            matches.Add(new Match
            {
                TournamentId = tournament.Id,
                Player1Id = userPlayer.Id,
                Player2Id = aiOpponents[0].Id,
                SurfaceId = surface.Id,
                Round = 1,
                MatchOrder = 1,
                IsCompleted = false,
                Player1Sets = 0,
                Player2Sets = 0
            });

            for (int i = 1; i < 8; i++)
            {
                matches.Add(new Match
                {
                    TournamentId = tournament.Id,
                    Player1Id = aiOpponents[i * 2 - 1].Id,
                    Player2Id = aiOpponents[i * 2].Id,
                    SurfaceId = surface.Id,
                    Round = 1,
                    MatchOrder = i + 1,
                    IsCompleted = false,
                    Player1Sets = 0,
                    Player2Sets = 0
                });
            }

            for (int i = 0; i < 4; i++)
            {
                matches.Add(new Match
                {
                    TournamentId = tournament.Id,
                    Player1Id = tbdPlayer.Id,
                    Player2Id = tbdPlayer.Id,
                    SurfaceId = surface.Id,
                    Round = 2,
                    MatchOrder = i + 1,
                    IsCompleted = false,
                    Player1Sets = 0,
                    Player2Sets = 0
                });
            }

            for (int i = 0; i < 2; i++)
            {
                matches.Add(new Match
                {
                    TournamentId = tournament.Id,
                    Player1Id = tbdPlayer.Id,
                    Player2Id = tbdPlayer.Id,
                    SurfaceId = surface.Id,
                    Round = 3,
                    MatchOrder = i + 1,
                    IsCompleted = false,
                    Player1Sets = 0,
                    Player2Sets = 0
                });
            }

            matches.Add(new Match
            {
                TournamentId = tournament.Id,
                Player1Id = tbdPlayer.Id,
                Player2Id = tbdPlayer.Id,
                SurfaceId = surface.Id,
                Round = 4,
                MatchOrder = 1,
                IsCompleted = false,
                Player1Sets = 0,
                Player2Sets = 0
            });

            DateTime startDateTime = DateTime.Today.Add(tournament.StartTime);
            DateTime endDateTime = DateTime.Today.Add(tournament.EndTime);
            TimeSpan tournamentDuration = endDateTime - startDateTime;
            TimeSpan interval = tournamentDuration / (matches.Count + 1);

            for (int i = 0; i < matches.Count; i++)
            {
                matches[i].StartTime = startDateTime.Add(interval * (i + 1));
            }

            List<int> playerIds = matches
                .SelectMany(m => new[] { m.Player1Id, m.Player2Id })
                .Where(id => id != tbdPlayer.Id)
                .Distinct()
                .ToList();

            List<int> existingPlayerIds = await _context.Players
                .Where(p => playerIds.Contains(p.Id))
                .Select(p => p.Id)
                .ToListAsync();

            if (existingPlayerIds.Count != playerIds.Count)
                throw new InvalidOperationException("Player not found in database");

            _context.Matches.AddRange(matches);
            await _context.SaveChangesAsync();
        }

        private async Task UpdateNextRoundMatch(Match completedMatch)
        {
            int winnerId = completedMatch.Player1Sets > completedMatch.Player2Sets
                ? completedMatch.Player1Id
                : completedMatch.Player2Id;

            int nextRound = completedMatch.Round + 1;
            int nextMatchOrder = (completedMatch.MatchOrder + 1) / 2;

            Match? nextMatch = await _context.Matches
                .FirstOrDefaultAsync(m =>
                    m.TournamentId == completedMatch.TournamentId &&
                    m.Round == nextRound &&
                    m.MatchOrder == nextMatchOrder);

            if (nextMatch == null)
                return;

            if (completedMatch.MatchOrder % 2 == 1)
                nextMatch.Player1Id = winnerId;
            else
                nextMatch.Player2Id = winnerId;

            await _context.SaveChangesAsync();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CompleteMatch(int id, int player1Sets, int player2Sets)
        {
            try
            {
                Match? match = await _context.Matches
                    .Include(m => m.Tournament)
                    .FirstOrDefaultAsync(m => m.Id == id);

                if (match == null)
                    return NotFound();

                if (player1Sets < 0 || player2Sets < 0 || (player1Sets == player2Sets))
                {
                    TempData["Error"] = "Invalid set scores. One player must win.";
                    return RedirectToAction(nameof(Details), new { id = match.TournamentId });
                }

                match.Player1Sets = player1Sets;
                match.Player2Sets = player2Sets;
                match.IsCompleted = true;
                match.EndTime = DateTime.Now;

                await _context.SaveChangesAsync();
                await UpdateNextRoundMatch(match);

                TempData["Success"] = "Match completed successfully.";
                return RedirectToAction(nameof(Details), new { id = match.TournamentId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error completing match");
                TempData["Error"] = "An error occurred while completing the match.";
                return RedirectToAction(nameof(Index));
            }
        }

        private async Task EnsureMatchesCompleted(int tournamentId)
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

                foreach (Match match in incompleteMatches)
                {
                    Random random = new Random();
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
                    await UpdateNextRoundMatch(match);
                }

                await _context.SaveChangesAsync();
                _logger.LogInformation($"Auto-completed {incompleteMatches.Count} matches for tournament {tournament.Id}");
            }
        }
    }
}