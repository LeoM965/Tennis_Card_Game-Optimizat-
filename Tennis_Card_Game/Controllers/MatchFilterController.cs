using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Tennis_Card_Game.Data;
using Tennis_Card_Game.Services;

namespace Tennis_Card_Game.Controllers
{
    public class MatchFilterController : Controller
    {
        private readonly Tennis_Card_GameContext _context;
        private readonly IMatchService _matchService;

        public MatchFilterController(Tennis_Card_GameContext context, IMatchService matchService)
        {
            _context = context;
            _matchService = matchService;
        }

        public async Task<IActionResult> MatchesByTournament(int tournamentId)
        {
            var tournament = await _context.Tournaments
                .Include(t => t.Surface)
                .FirstOrDefaultAsync(t => t.Id == tournamentId);

            if (tournament == null)
            {
                return NotFound();
            }

            var matches = await _matchService.GetMatchesQuery()
                .Where(m => m.TournamentId == tournamentId)
                .OrderBy(m => m.StartTime)
                .ToListAsync();

            ViewBag.TournamentName = tournament.Name;
            ViewBag.TournamentSurface = tournament.Surface?.Name;
            ViewBag.TournamentLevel = tournament.Level;

            return View(matches);
        }

        public async Task<IActionResult> MatchesByPlayer(int playerId)
        {
            var player = await _context.Players
                .FirstOrDefaultAsync(p => p.Id == playerId);

            if (player == null)
            {
                return NotFound();
            }

            var matches = await _matchService.GetMatchesQuery()
                .Where(m => m.Player1Id == playerId || m.Player2Id == playerId)
                .OrderByDescending(m => m.StartTime)
                .ToListAsync();

            ViewBag.PlayerName = player.Name;

            int wins = matches.Count(m =>
                (m.Player1Id == playerId && m.IsCompleted && m.Player1Sets > m.Player2Sets) ||
                (m.Player2Id == playerId && m.IsCompleted && m.Player2Sets > m.Player1Sets));

            int completedMatches = matches.Count(m => m.IsCompleted);

            ViewBag.WinCount = wins;
            ViewBag.MatchCount = completedMatches;
            ViewBag.WinRate = completedMatches > 0 ? (double)wins / completedMatches * 100 : 0;

            return View(matches);
        }

        public async Task<IActionResult> MatchesBySurface(int surfaceId)
        {
            var surface = await _context.Surfaces
                .FirstOrDefaultAsync(s => s.Id == surfaceId);

            if (surface == null)
            {
                return NotFound();
            }

            var matches = await _matchService.GetMatchesQuery()
                .Where(m => m.SurfaceId == surfaceId)
                .OrderByDescending(m => m.StartTime)
                .ToListAsync();

            ViewBag.SurfaceName = surface.Name;

            return View(matches);
        }

        public async Task<IActionResult> MatchesByWeather(int weatherId)
        {
            var weather = await _context.WeatherConditions
                .FirstOrDefaultAsync(w => w.Id == weatherId);

            if (weather == null)
            {
                return NotFound();
            }

            var matches = await _matchService.GetMatchesQuery()
                .Where(m => m.WeatherConditionId == weatherId)
                .OrderByDescending(m => m.StartTime)
                .ToListAsync();

            ViewBag.WeatherName = weather.Name;

            return View(matches);
        }
    }
}