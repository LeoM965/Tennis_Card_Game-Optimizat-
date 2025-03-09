using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Data;
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
                _logger.LogInformation("Loading tournaments for Index view");

                var tournaments = await _context.Tournaments
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

                _logger.LogInformation($"Successfully loaded {tournaments.Count} tournaments");
                return View(tournaments);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading tournaments: {ErrorMessage}", ex.Message);
                TempData["Error"] = "An error occurred while loading tournaments.";
                return View(new List<TournamentViewModel>());
            }
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                _logger.LogWarning("Tournament details requested without ID");
                return NotFound();
            }

            try
            {
                _logger.LogInformation($"Loading details for tournament ID {id}");

                var tournament = await _context.Tournaments
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
                {
                    _logger.LogWarning($"Tournament with ID {id} not found");
                    return NotFound();
                }

                _logger.LogInformation($"Successfully loaded details for tournament ID {id}");
                return View(tournament);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading tournament details: {ErrorMessage}", ex.Message);
                TempData["Error"] = "An error occurred while loading tournament details.";
                return RedirectToAction(nameof(Index));
            }
        }

        public async Task<IActionResult> Current()
        {
            try
            {
                var currentTime = DateTime.Now.TimeOfDay;
                var today = DateTime.Today;

                _logger.LogInformation("Fetching current tournaments. Time: {CurrentTime}", currentTime);

                var currentTournaments = await _context.Tournaments
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

                _logger.LogInformation($"Found {currentTournaments.Count} current tournaments");
                return View(currentTournaments);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching current tournaments: {ErrorMessage}", ex.Message);
                TempData["Error"] = "An error occurred while loading current tournaments.";
                return View(new List<TournamentViewModel>());
            }
        }

        [HttpGet]
        public async Task<IActionResult> Join(int? id)
        {
            if (id == null)
            {
                _logger.LogWarning("Join action called without tournament ID");
                return NotFound();
            }

            try
            {
                _logger.LogInformation($"Attempting to join tournament ID {id}");

                var tournament = await _context.Tournaments
                    .Include(t => t.Surface)
                    .FirstOrDefaultAsync(t => t.Id == id);

                if (tournament == null)
                {
                    _logger.LogWarning($"Tournament with ID {id} not found");
                    return NotFound();
                }

                var currentTime = DateTime.Now;
                var tournamentStartDateTime = DateTime.Today.Add(tournament.StartTime);
                var registrationOpenTime = tournamentStartDateTime.AddHours(-1);

                _logger.LogInformation("Join check - CurrentTime: {CurrentTime}, TournamentStart: {TournamentStart}",
                    currentTime, tournamentStartDateTime);

                if (currentTime < registrationOpenTime)
                {
                    _logger.LogInformation($"Registration not open yet for tournament ID {id}");
                    TempData["Error"] = "Registration is not open yet. You can register 1 hour before the tournament starts.";
                    return RedirectToAction(nameof(Details), new { id = id });
                }

                DateTime tournamentEndDateTime;
                if (tournament.EndTime.TotalHours == 0)
                {
                    tournamentEndDateTime = DateTime.Today.AddDays(1);
                }
                else
                {
                    tournamentEndDateTime = DateTime.Today.Add(tournament.EndTime);
                }

                var registrationCloseTime = tournamentEndDateTime.AddMinutes(-5);

                if (currentTime > registrationCloseTime)
                {
                    _logger.LogInformation($"Registration closed for tournament ID {id}");
                    TempData["Error"] = "Registration is closed. The tournament is about to end.";
                    return RedirectToAction(nameof(Details), new { id = id });
                }

                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId))
                {
                    _logger.LogWarning("User not logged in while attempting to join tournament");
                    TempData["Error"] = "You must be logged in to join a tournament.";
                    return RedirectToAction(nameof(Details), new { id = id });
                }

                var userPlayer = await _context.Players.FirstOrDefaultAsync(p => p.UserId == userId);
                if (userPlayer == null)
                {
                    _logger.LogWarning($"No player found for user ID {userId}");
                    TempData["Error"] = "You don't have a player to participate in the tournament!";
                    return RedirectToAction(nameof(Details), new { id = id });
                }

                var existingMatch = await _context.Matches
                    .AnyAsync(m => m.TournamentId == tournament.Id &&
                             (m.Player1Id == userPlayer.Id || m.Player2Id == userPlayer.Id));

                if (existingMatch)
                {
                    _logger.LogInformation($"User already registered in tournament ID {id}");
                    TempData["Error"] = "You are already registered in this tournament!";
                    return RedirectToAction(nameof(Details), new { id = id });
                }

                var viewModel = new TournamentJoinViewModel
                {
                    TournamentId = tournament.Id,
                    TournamentName = tournament.Name,
                    Surface = tournament.Surface.Name,
                    StartTime = tournament.StartTime,
                    Level = tournament.Level,
                    XpReward = tournament.XpReward,
                    CoinReward = tournament.CoinReward
                };

                _logger.LogInformation($"Showing join view for tournament ID {id}");
                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Join action: {ErrorMessage}", ex.Message);
                TempData["Error"] = "An error occurred while trying to join the tournament.";
                return RedirectToAction(nameof(Details), new { id = id });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> JoinConfirm(int tournamentId)
        {
            try
            {
                var tournament = await _context.Tournaments
                    .Include(t => t.Surface)
                    .FirstOrDefaultAsync(t => t.Id == tournamentId);

                if (tournament == null)
                {
                    return NotFound();
                }

                var currentTime = DateTime.Now;
                var tournamentStartDateTime = DateTime.Today.Add(tournament.StartTime);
                var registrationOpenTime = tournamentStartDateTime.AddHours(-1);

                if (currentTime < registrationOpenTime)
                {
                    TempData["Error"] = "Registration is not open yet. You can register 1 hour before the tournament starts.";
                    return RedirectToAction(nameof(Details), new { id = tournamentId });
                }

                DateTime tournamentEndDateTime;
                if (tournament.EndTime.TotalHours == 0)
                {
                    tournamentEndDateTime = DateTime.Today.AddDays(1);
                }
                else
                {
                    tournamentEndDateTime = DateTime.Today.Add(tournament.EndTime);
                }

                var registrationCloseTime = tournamentEndDateTime.AddMinutes(-5);

                if (currentTime > registrationCloseTime)
                {
                    TempData["Error"] = "Registration is closed. The tournament is about to end.";
                    return RedirectToAction(nameof(Details), new { id = tournamentId });
                }

                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId))
                {
                    TempData["Error"] = "You must be logged in to join a tournament.";
                    return RedirectToAction(nameof(Details), new { id = tournamentId });
                }

                var userPlayer = await _context.Players
                    .Where(p => p.UserId == userId)
                    .FirstOrDefaultAsync();

                if (userPlayer == null)
                {
                    TempData["Error"] = "You don't have a player to participate in the tournament!";
                    return RedirectToAction(nameof(Details), new { id = tournamentId });
                }

                var existingMatch = await _context.Matches
                    .AnyAsync(m => m.TournamentId == tournamentId &&
                             (m.Player1Id == userPlayer.Id || m.Player2Id == userPlayer.Id));

                if (existingMatch)
                {
                    TempData["Error"] = "You are already participating in this tournament!";
                    return RedirectToAction(nameof(Details), new { id = tournamentId });
                }

                var existingRegistration = await _context.TournamentRegistrations
                    .AnyAsync(r => r.TournamentId == tournamentId && r.PlayerId == userPlayer.Id);

                if (existingRegistration)
                {
                    TempData["Error"] = "You are already registered in this tournament!";
                    return RedirectToAction(nameof(Details), new { id = tournamentId });
                }

                var surface = await _context.Surfaces.FindAsync(tournament.SurfaceId);
                if (surface == null)
                {
                    TempData["Error"] = "The playing surface was not found!";
                    return RedirectToAction(nameof(Details), new { id = tournamentId });
                }

                var aiOpponents = await _context.Players
     .Where(p => p.UserId == null)
     .Include(p => p.PlayingStyle)
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
                    var registration = new TournamentRegistration
                    {
                        TournamentId = tournamentId,
                        PlayerId = userPlayer.Id,
                        RegistrationDate = DateTime.Now
                    };

                    _context.TournamentRegistrations.Add(registration);

                    await _context.SaveChangesAsync();

                    var matches = new List<Match>();

                    for (int i = 0; i < 8; i++)
                    {
                        var player1Id = i == 0 ? userPlayer.Id : aiOpponents[i - 1].Id;
                        var player2Id = aiOpponents[i + 7].Id;

                        var match = new Match
                        {
                            TournamentId = tournament.Id,
                            Player1Id = player1Id,
                            Player2Id = player2Id,
                            SurfaceId = surface.Id,
                            Round = 1,
                            MatchOrder = i + 1,
                            StartTime = DateTime.Today.Add(tournament.StartTime.Add(TimeSpan.FromMinutes(i * 15))),
                            IsCompleted = false,
                            Player1Sets = 0,
                            Player2Sets = 0
                        };

                        matches.Add(match);
                    }

                    _context.Matches.AddRange(matches);
                    await _context.SaveChangesAsync();

                    TempData["Success"] = "You have successfully registered for the tournament and matches have been generated!";
                    return RedirectToAction(nameof(Details), new { id = tournamentId });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error during tournament join: {Message}", ex.Message);
                    TempData["Error"] = $"Error joining tournament: {ex.Message}";
                    return RedirectToAction(nameof(Details), new { id = tournamentId });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in JoinConfirm: {Message}", ex.Message);
                TempData["Error"] = $"An unexpected error occurred: {ex.Message}";
                return RedirectToAction(nameof(Details), new { id = tournamentId });
            }
        }

        private async Task GenerateTournamentMatches(Tournament tournament, Player userPlayer, List<Player> aiOpponents, Surface surface)
        {
            _logger.LogInformation($"Generating matches for tournament ID {tournament.Id}");

            if (aiOpponents.Count < 15)
            {
                throw new InvalidOperationException("Not enough AI players for tournament");
            }

            var allPlayerIds = new List<int> { userPlayer.Id };
            allPlayerIds.AddRange(aiOpponents.Select(o => o.Id));

            var matches = new List<Match>();

            for (int i = 0; i < 8; i++)
            {
                var player1Id = allPlayerIds[i];
                var player2Id = allPlayerIds[15 - i];

                var player1Exists = await _context.Players.AnyAsync(p => p.Id == player1Id);
                var player2Exists = await _context.Players.AnyAsync(p => p.Id == player2Id);

                if (!player1Exists || !player2Exists)
                {
                    _logger.LogError("Player not found: Player1={Player1Id}, Player2={Player2Id}", player1Id, player2Id);
                    throw new InvalidOperationException("Player not found in database");
                }

                var match = new Match
                {
                    TournamentId = tournament.Id,
                    Player1Id = player1Id,
                    Player2Id = player2Id,
                    SurfaceId = surface.Id,
                    Round = 1,
                    MatchOrder = i + 1,
                    StartTime = DateTime.Today.Add(tournament.StartTime.Add(TimeSpan.FromMinutes(i * 15))),
                    IsCompleted = false,
                    Player1Sets = 0,
                    Player2Sets = 0
                };

                matches.Add(match);
            }

            foreach (var match in matches)
            {
                _context.Matches.Add(match);
            }

            await _context.SaveChangesAsync();
            _logger.LogInformation($"Successfully created {matches.Count} matches for tournament ID {tournament.Id}");
        }
    }
}