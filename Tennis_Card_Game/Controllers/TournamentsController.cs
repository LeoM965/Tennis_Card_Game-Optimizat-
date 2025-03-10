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
            try
            {
                var currentTime = DateTime.Now.TimeOfDay;

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

                return View(currentTournaments);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching current tournaments");
                TempData["Error"] = "An error occurred while loading current tournaments.";
                return View(new List<TournamentViewModel>());
            }
        }

        [HttpGet]
        public async Task<IActionResult> Join(int? id)
        {
            if (id == null)
                return NotFound();

            try
            {
                var tournament = await _context.Tournaments
                    .Include(t => t.Surface)
                    .FirstOrDefaultAsync(t => t.Id == id);

                if (tournament == null)
                    return NotFound();

                var currentTime = DateTime.Now;
                var tournamentStartDateTime = DateTime.Today.Add(tournament.StartTime);
                var registrationOpenTime = tournamentStartDateTime.AddHours(-1);

                if (currentTime < registrationOpenTime)
                {
                    TempData["Error"] = "Registration is not open yet. You can register 1 hour before the tournament starts.";
                    return RedirectToAction(nameof(Details), new { id });
                }

                DateTime tournamentEndDateTime = tournament.EndTime.TotalHours == 0
                    ? DateTime.Today.AddDays(1)
                    : DateTime.Today.Add(tournament.EndTime);

                var registrationCloseTime = tournamentEndDateTime.AddMinutes(-5);

                if (currentTime > registrationCloseTime)
                {
                    TempData["Error"] = "Registration is closed. The tournament is about to end.";
                    return RedirectToAction(nameof(Details), new { id });
                }

                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId))
                {
                    TempData["Error"] = "You must be logged in to join a tournament.";
                    return RedirectToAction(nameof(Details), new { id });
                }

                var userPlayer = await _context.Players.FirstOrDefaultAsync(p => p.UserId == userId);
                if (userPlayer == null)
                {
                    TempData["Error"] = "You don't have a player to participate in the tournament!";
                    return RedirectToAction(nameof(Details), new { id });
                }

                var existingMatch = await _context.Matches
                    .AnyAsync(m => m.TournamentId == tournament.Id &&
                             (m.Player1Id == userPlayer.Id || m.Player2Id == userPlayer.Id));

                if (existingMatch)
                {
                    TempData["Error"] = "You are already registered in this tournament!";
                    return RedirectToAction(nameof(Details), new { id });
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
                var tournament = await _context.Tournaments
                    .Include(t => t.Surface)
                    .FirstOrDefaultAsync(t => t.Id == tournamentId);

                if (tournament == null)
                    return NotFound();

                var currentTime = DateTime.Now;
                var tournamentStartDateTime = DateTime.Today.Add(tournament.StartTime);
                var registrationOpenTime = tournamentStartDateTime.AddHours(-1);

                if (currentTime < registrationOpenTime)
                {
                    TempData["Error"] = "Registration is not open yet. You can register 1 hour before the tournament starts.";
                    return RedirectToAction(nameof(Details), new { id = tournamentId });
                }

                DateTime tournamentEndDateTime = tournament.EndTime.TotalHours == 0
                    ? DateTime.Today.AddDays(1)
                    : DateTime.Today.Add(tournament.EndTime);

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
                    var registration = new TournamentRegistration
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

        private async Task GenerateTournamentMatches(Tournament tournament, Player userPlayer, List<Player> aiOpponents, Surface surface)
        {
            if (aiOpponents.Count < 15)
                throw new InvalidOperationException("Not enough AI players for tournament");

            var matches = new List<Match>
            {
                new Match
                {
                    TournamentId = tournament.Id,
                    Player1Id = userPlayer.Id,
                    Player2Id = aiOpponents[7].Id,
                    SurfaceId = surface.Id,
                    Round = 1,
                    MatchOrder = 1,
                    StartTime = DateTime.Today.Add(tournament.StartTime),
                    IsCompleted = false,
                    Player1Sets = 0,
                    Player2Sets = 0
                }
            };

            for (int i = 1; i < 8; i++)
            {
                matches.Add(new Match
                {
                    TournamentId = tournament.Id,
                    Player1Id = aiOpponents[i - 1].Id,
                    Player2Id = aiOpponents[i + 7].Id,
                    SurfaceId = surface.Id,
                    Round = 1,
                    MatchOrder = i + 1,
                    StartTime = DateTime.Today.Add(tournament.StartTime.Add(TimeSpan.FromMinutes(i * 15))),
                    IsCompleted = false,
                    Player1Sets = 0,
                    Player2Sets = 0
                });
            }

            var playerIds = matches.SelectMany(m => new[] { m.Player1Id, m.Player2Id }).Distinct().ToList();
            var existingPlayerIds = await _context.Players
                .Where(p => playerIds.Contains(p.Id))
                .Select(p => p.Id)
                .ToListAsync();

            if (existingPlayerIds.Count != playerIds.Count)
                throw new InvalidOperationException("Player not found in database");

            _context.Matches.AddRange(matches);
            await _context.SaveChangesAsync();
        }
    }
}