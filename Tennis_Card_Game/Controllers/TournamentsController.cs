using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Tennis_Card_Game.Data;
using Tennis_Card_Game.Models;
using Tennis_Card_Game.ViewModel;

namespace TennisCardBattle.Controllers
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
            _logger.LogInformation("Loading tournaments for Index view.");
            try
            {
                List<TournamentViewModel> tournaments = await _context.Tournaments
                    .Include(t => t.Surface)
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
                _logger.LogInformation($"Loaded {tournaments.Count} tournaments for Index view.");
                return View(tournaments);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading tournaments for Index view: {ErrorMessage}", ex.Message);
                TempData["Error"] = "An error occurred while loading tournaments.";
                return View(new List<TournamentViewModel>());
            }
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                _logger.LogWarning("Details action called without ID.");
                return NotFound();
            }
            _logger.LogInformation($"Loading details for tournament ID {id}.");
            try
            {
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
                {
                    _logger.LogWarning($"Tournament with ID {id} not found.");
                    return NotFound();
                }
                _logger.LogInformation($"Successfully loaded details for tournament ID {id}.");
                return View(tournament);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading details for tournament ID {TournamentId}: {ErrorMessage}", id, ex.Message);
                TempData["Error"] = "An error occurred while loading tournament details.";
                return RedirectToAction(nameof(Index));
            }
        }

        public async Task<IActionResult> Current()
        {
            TimeSpan currentTime = DateTime.Now.TimeOfDay;
            DateTime today = DateTime.Today;
            _logger.LogInformation("Fetching current tournaments. Current time: {CurrentTime}, Today: {Today}", currentTime, today);
            try
            {
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
                _logger.LogInformation($"Found {currentTournaments.Count} current tournaments.");
                foreach (var t in currentTournaments)
                {
                    _logger.LogDebug("Current tournament: {TournamentName}, ID: {TournamentId}, StartTime: {StartTime}, EndTime: {EndTime}",
                        t.Name, t.Id, t.StartTime, t.EndTime);
                }
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
                _logger.LogWarning("Join action called without ID.");
                return NotFound();
            }
            _logger.LogInformation($"Attempting to join tournament ID {id}.");
            try
            {
                var tournament = await _context.Tournaments
                    .Include(t => t.Surface)
                    .FirstOrDefaultAsync(t => t.Id == id);
                if (tournament == null)
                {
                    _logger.LogWarning($"Tournament with ID {id} not found.");
                    return NotFound();
                }
                DateTime currentTime = DateTime.Now;
                DateTime tournamentStartDateTime = DateTime.Today.Add(tournament.StartTime);
                DateTime registrationOpenTime = tournamentStartDateTime.AddHours(-2);
                _logger.LogInformation("Join check - CurrentTime: {CurrentTime}, TournamentStart: {TournamentStart}, RegistrationOpen: {RegistrationOpen}",
                    currentTime, tournamentStartDateTime, registrationOpenTime);
                if (currentTime < registrationOpenTime)
                {
                    _logger.LogInformation($"Registration not open yet for tournament ID {id}. Current time: {currentTime}, Registration opens: {registrationOpenTime}");
                    TempData["Error"] = "Registration is not open yet. You can register 1 hour before the tournament starts.";
                    return RedirectToAction(nameof(Details), new { id = id });
                }
                DateTime tournamentEndDateTime = DateTime.Today.Add(tournament.EndTime);
                DateTime registrationCloseTime = tournamentEndDateTime.AddMinutes(-5);
                _logger.LogInformation("Join check - TournamentEnd: {TournamentEnd}, RegistrationClose: {RegistrationClose}",
                    tournamentEndDateTime, registrationCloseTime);
                if (currentTime > registrationCloseTime)
                {
                    _logger.LogInformation($"Registration closed for tournament ID {id}. Current time: {currentTime}, Registration closed: {registrationCloseTime}");
                    TempData["Error"] = "Registration is closed. The tournament is about to end.";
                    return RedirectToAction(nameof(Details), new { id = id });
                }
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                _logger.LogInformation("User ID from claims: {UserId}", userId ?? "null");
                if (string.IsNullOrEmpty(userId))
                {
                    _logger.LogWarning("User not logged in while attempting to join tournament.");
                    TempData["Error"] = "You must be logged in to join a tournament.";
                    return RedirectToAction(nameof(Details), new { id = id });
                }
                var userPlayer = await _context.Players
                    .FirstOrDefaultAsync(p => p.UserId == userId);
                _logger.LogInformation("Player found for user ID {UserId}: {PlayerFound}", userId, userPlayer != null);
                if (userPlayer == null)
                {
                    _logger.LogWarning($"No player found for user ID {userId}.");
                    TempData["Error"] = "You don't have a player to participate in the tournament!";
                    return RedirectToAction(nameof(Details), new { id = id });
                }
                _logger.LogInformation("Checking if player ID {PlayerId} is already in tournament {TournamentId}", userPlayer.Id, id);
                var existingMatches = await _context.Matches
                    .AnyAsync(m => m.TournamentId == tournament.Id &&
                                 (m.Player1Id == userPlayer.Id || m.Player2Id == userPlayer.Id));
                if (existingMatches)
                {
                    _logger.LogInformation($"User already registered in tournament ID {id}.");
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
                _logger.LogInformation($"Returning join view for tournament ID {id}.");
                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Join action for tournament ID {TournamentId}: {ErrorMessage}", id, ex.Message);
                TempData["Error"] = "An error occurred while trying to join the tournament.";
                return RedirectToAction(nameof(Details), new { id = id });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> JoinConfirm(int tournamentId)
        {
            _logger.LogInformation($"Confirming join for tournament ID {tournamentId}.");
            try
            {
                var tournament = await _context.Tournaments
                    .Include(t => t.Surface)
                    .FirstOrDefaultAsync(t => t.Id == tournamentId);
                if (tournament == null)
                {
                    _logger.LogWarning($"Tournament with ID {tournamentId} not found during confirmation.");
                    return NotFound();
                }
                _logger.LogInformation("Tournament found: {TournamentName}, Surface ID: {SurfaceId}",
                    tournament.Name, tournament.SurfaceId);

                DateTime currentTime = DateTime.Now;
                DateTime tournamentStartDateTime = DateTime.Today.Add(tournament.StartTime);
                DateTime registrationOpenTime = tournamentStartDateTime.AddHours(-1);
                _logger.LogInformation("JoinConfirm check - CurrentTime: {CurrentTime}, TournamentStart: {TournamentStart}, RegistrationOpen: {RegistrationOpen}",
                    currentTime, tournamentStartDateTime, registrationOpenTime);
                if (currentTime < registrationOpenTime)
                {
                    _logger.LogInformation($"Registration not open yet for tournament ID {tournamentId} during confirmation.");
                    TempData["Error"] = "Registration is not open yet. You can register 1 hour before the tournament starts.";
                    return RedirectToAction(nameof(Details), new { id = tournamentId });
                }

                DateTime tournamentEndDateTime = DateTime.Today.Add(tournament.EndTime);
                DateTime registrationCloseTime = tournamentEndDateTime.AddMinutes(-5);
                _logger.LogInformation("JoinConfirm check - TournamentEnd: {TournamentEnd}, RegistrationClose: {RegistrationClose}",
                    tournamentEndDateTime, registrationCloseTime);
                if (currentTime > registrationCloseTime)
                {
                    _logger.LogInformation($"Registration closed for tournament ID {tournamentId} during confirmation.");
                    TempData["Error"] = "Registration is closed. The tournament is about to end.";
                    return RedirectToAction(nameof(Details), new { id = tournamentId });
                }

                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                _logger.LogInformation("User ID from claims in JoinConfirm: {UserId}", userId ?? "null");
                if (string.IsNullOrEmpty(userId))
                {
                    _logger.LogWarning("User not logged in during confirmation.");
                    TempData["Error"] = "You must be logged in to join a tournament.";
                    return RedirectToAction(nameof(Details), new { id = tournamentId });
                }

                var userExists = await _context.Users.AnyAsync(u => u.Id == userId);
                _logger.LogInformation("User exists check: {UserExists} for ID {UserId}", userExists, userId);
                if (!userExists)
                {
                    _logger.LogWarning($"User account not found for ID {userId}.");
                    TempData["Error"] = "User account not found in the system.";
                    return RedirectToAction(nameof(Details), new { id = tournamentId });
                }

                var userPlayer = await _context.Players
                    .FirstOrDefaultAsync(p => p.UserId == userId);
                _logger.LogInformation("Player found for user ID {UserId} in JoinConfirm: {PlayerFound}",
                    userId, userPlayer != null);
                if (userPlayer == null)
                {
                    _logger.LogWarning($"No player found for user ID {userId} during confirmation.");
                    TempData["Error"] = "You don't have a player to participate in the tournament!";
                    return RedirectToAction(nameof(Details), new { id = tournamentId });
                }
                _logger.LogInformation("Player ID: {PlayerId}, Name: {PlayerName}", userPlayer.Id, userPlayer.Name);

                var existingMatches = await _context.Matches
                    .AnyAsync(m => m.TournamentId == tournamentId &&
                                   (m.Player1Id == userPlayer.Id || m.Player2Id == userPlayer.Id));
                _logger.LogInformation("Existing matches check: {ExistingMatches}", existingMatches);
                if (existingMatches)
                {
                    _logger.LogInformation($"User already participating in tournament ID {tournamentId}.");
                    TempData["Error"] = "You are already participating in this tournament!";
                    return RedirectToAction(nameof(Details), new { id = tournamentId });
                }

                var existingRegistration = await _context.TournamentRegistrations
                    .AnyAsync(r => r.TournamentId == tournamentId && r.PlayerId == userPlayer.Id);
                _logger.LogInformation("Existing registration check: {ExistingRegistration}", existingRegistration);
                if (existingRegistration)
                {
                    _logger.LogInformation($"User already registered in tournament ID {tournamentId}.");
                    TempData["Error"] = "You are already registered in this tournament!";
                    return RedirectToAction(nameof(Details), new { id = tournamentId });
                }

                var surface = await _context.Surfaces.FindAsync(tournament.SurfaceId);
                _logger.LogInformation("Surface found: {SurfaceFound}, ID: {SurfaceId}",
                    surface != null, tournament.SurfaceId);
                if (surface == null)
                {
                    _logger.LogError($"Surface not found for tournament ID {tournamentId}.");
                    TempData["Error"] = "The playing surface was not found!";
                    return RedirectToAction(nameof(Details), new { id = tournamentId });
                }

                _logger.LogInformation("Fetching AI opponents for tournament");
                var aiOpponents = await _context.Players
                    .Where(p => p.UserId == null)
                    .OrderBy(p => Guid.NewGuid())
                    .Take(15)
                    .ToListAsync();
                _logger.LogInformation("AI opponents found: {OpponentCount}", aiOpponents.Count);

                if (aiOpponents.Count < 15)
                {
                    _logger.LogError($"Not enough AI players for tournament ID {tournamentId}. Found: {aiOpponents.Count}");
                    TempData["Error"] = "There are not enough AI players in the database to generate a tournament!";
                    return RedirectToAction(nameof(Details), new { id = tournamentId });
                }

                _logger.LogInformation("Starting database transaction for tournament registration");
                using (var transaction = await _context.Database.BeginTransactionAsync())
                {
                    try
                    {
                        _logger.LogInformation("Creating tournament registration record");
                        var registration = new TournamentRegistration
                        {
                            TournamentId = tournamentId,
                            PlayerId = userPlayer.Id,
                            RegistrationDate = DateTime.Now
                        };
                        _context.TournamentRegistrations.Add(registration);
                        await _context.SaveChangesAsync();

                        _logger.LogInformation("Registration record saved, generating tournament matches");
                        await GenerateTournamentMatches(tournament, userPlayer, aiOpponents, surface);

                        _logger.LogInformation("Committing transaction");
                        await transaction.CommitAsync();
                        _logger.LogInformation($"User successfully registered for tournament ID {tournamentId}.");
                        TempData["Success"] = "You have successfully registered for the tournament and matches have been generated!";
                        return RedirectToAction(nameof(Details), new { id = tournamentId });
                    }
                    catch (Exception ex)
                    {
                        await transaction.RollbackAsync();
                        _logger.LogError(ex, $"Error joining tournament ID {tournamentId}: {ex.Message}");
                        if (ex.InnerException != null)
                        {
                            _logger.LogError(ex.InnerException, "Inner exception: {InnerMessage}", ex.InnerException.Message);
                        }
                        TempData["Error"] = $"Error joining tournament: {ex.Message}";
                        if (ex.InnerException != null)
                        {
                            TempData["Error"] += $" Details: {ex.InnerException.Message}";
                        }
                        return RedirectToAction(nameof(Details), new { id = tournamentId });
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in JoinConfirm for tournament ID {TournamentId}: {ErrorMessage}",
                    tournamentId, ex.Message);
                TempData["Error"] = "An unexpected error occurred while joining the tournament.";
                return RedirectToAction(nameof(Details), new { id = tournamentId });
            }
        }

        private async Task GenerateTournamentMatches(Tournament tournament, Player userPlayer, List<Player> aiOpponents, Surface surface)
        {
            _logger.LogInformation($"Generating matches for tournament ID {tournament.Id}.");

            if (aiOpponents.Count < 15)
            {
                _logger.LogError($"Not enough AI players for tournament ID {tournament.Id}");
                throw new InvalidOperationException("There are not enough AI players to generate a tournament");
            }

            var allPlayerIds = new List<int> { userPlayer.Id };
            allPlayerIds.AddRange(aiOpponents.Select(o => o.Id));

            var matches = new List<Match>();
            for (int i = 0; i < 8; i++)
            {
                var player1Id = allPlayerIds[i];
                var player2Id = allPlayerIds[15 - i];

                var match = new Match
                {
                    TournamentId = tournament.Id,
                    Player1Id = player1Id,
                    Player2Id = player2Id,
                    SurfaceId = surface.Id,
                    Round = 1,
                    MatchOrder = i + 1,
                    StartTime = DateTime.Today.Add(tournament.StartTime.Add(TimeSpan.FromMinutes(i * 15))),
                    IsCompleted = false
                };
                matches.Add(match);
            }

            _context.Matches.AddRange(matches);
            await _context.SaveChangesAsync();
            _logger.LogInformation($"Generated {matches.Count} matches for tournament ID {tournament.Id}.");
        }
    }
}