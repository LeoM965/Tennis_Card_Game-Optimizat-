using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Tennis_Card_Game.Data;

namespace Tennis_Card_Game.Controllers
{
    public class TournamentBracketController : MatchesController
    {
        private readonly Tennis_Card_GameContext _context;

        public TournamentBracketController(Tennis_Card_GameContext context) : base(context)
        {
            _context = context;
        }

        public async Task<IActionResult> TournamentBracket(int tournamentId)
        {
            var tournament = await _context.Tournaments
                .FirstOrDefaultAsync(t => t.Id == tournamentId);

            if (tournament == null)
            {
                return NotFound();
            }

            var matches = await GetMatchesQuery()
                .Where(m => m.TournamentId == tournamentId)
                .OrderBy(m => m.Round)
                .ThenBy(m => m.MatchOrder)
                .ToListAsync();

            ViewBag.TournamentName = tournament.Name;

            return View(matches);
        }
    }
}