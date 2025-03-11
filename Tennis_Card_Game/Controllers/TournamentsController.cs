using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Tennis_Card_Game.Data;
using Tennis_Card_Game.Models;
using Tennis_Card_Game.Services;
using Tennis_Card_Game.ViewModel;

namespace Tennis_Card_Game.Controllers
{
    public class TournamentsController : Controller
    {
        private readonly Tennis_Card_GameContext _context;
        private readonly ITournamentService _tournamentService;
        private readonly IMatchService _matchService;

        public TournamentsController(
            Tennis_Card_GameContext context,
            ITournamentService tournamentService,
            IMatchService matchService)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _tournamentService = tournamentService ?? throw new ArgumentNullException(nameof(tournamentService));
            _matchService = matchService ?? throw new ArgumentNullException(nameof(matchService));
        }

        public async Task<IActionResult> Index()
        {
            try
            {
                var tournaments = await _tournamentService.GetAllTournamentsAsync();
                return View(tournaments);
            }
            catch (Exception ex)
            {
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
                await _tournamentService.EnsureMatchesCompletedAsync(id.Value);

                var tournament = await _tournamentService.GetTournamentDetailsAsync(id.Value);

                if (tournament == null)
                    return NotFound();

                return View(tournament);
            }
            catch (Exception ex)
            {
                TempData["Error"] = "An error occurred while loading tournament details.";
                return RedirectToAction(nameof(Index));
            }
        }

        public async Task<IActionResult> Current()
        {
            try
            {
                var currentTournaments = await _tournamentService.GetCurrentTournamentsAsync();
                return View(currentTournaments);
            }
            catch (Exception ex)
            {
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
                string? userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (userId == null)
                {
                    TempData["Error"] = "You must be logged in to join a tournament.";
                    return RedirectToAction(nameof(Details), new { id });
                }

                var (isEligible, errorMessage, player, tournament) =
                    await _tournamentService.CheckTournamentEligibilityAsync(id.Value, userId);

                if (!isEligible || tournament == null)
                {
                    TempData["Error"] = errorMessage;
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

                var result = await _tournamentService.JoinTournamentAsync(tournamentId, userId);

                if (result.Success)
                {
                    TempData["Success"] = "You have successfully registered for the tournament and matches have been generated!";
                }
                else
                {
                    TempData["Error"] = result.ErrorMessage;
                }

                return RedirectToAction(nameof(Details), new { id = tournamentId });
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"An unexpected error occurred: {ex.Message}";
                return RedirectToAction(nameof(Details), new { id = tournamentId });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CompleteMatch(int id, int player1Sets, int player2Sets)
        {
            try
            {
                var result = await _matchService.CompleteMatchAsync(id, player1Sets, player2Sets);

                if (result.Success)
                {
                    TempData["Success"] = "Match completed successfully.";
                    return RedirectToAction(nameof(Details), new { id = result.TournamentId });
                }
                else
                {
                    TempData["Error"] = result.ErrorMessage;
                    return RedirectToAction(nameof(Index));
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = "An error occurred while completing the match.";
                return RedirectToAction(nameof(Index));
            }
        }
    }
}