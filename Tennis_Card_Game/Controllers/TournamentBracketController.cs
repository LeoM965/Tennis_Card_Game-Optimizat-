using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Tennis_Card_Game.Data;
using Tennis_Card_Game.Services;

namespace Tennis_Card_Game.Controllers
{
    public class TournamentBracketController : Controller
    {
        private readonly Tennis_Card_GameContext _context;
        private readonly IMatchService _matchService;

        public TournamentBracketController(Tennis_Card_GameContext context , IMatchService matchService) 
        {
            _context = context;
            _matchService = matchService;
        }

        public async Task<IActionResult> TournamentBracket(int tournamentId)
        {
            var tournament = await _context.Tournaments
                .FirstOrDefaultAsync(t => t.Id == tournamentId);

            if (tournament == null)
            {
                return NotFound();
            }

            var matches = await _matchService.GetMatchesQuery()
                .Where(m => m.TournamentId == tournamentId)
                .OrderBy(m => m.Round)
                .ThenBy(m => m.MatchOrder)
                .ToListAsync();

            ViewBag.TournamentName = tournament.Name;

            return View(matches);
        }
    }
}