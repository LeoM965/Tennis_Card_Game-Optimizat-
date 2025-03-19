using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Tennis_Card_Game.Data;
using Tennis_Card_Game.Services;

namespace Tennis_Card_Game.Controllers
{
    public class MatchesController : Controller
    {
        private readonly IMatchService _matchService;

        public MatchesController(IMatchService matchService)
        {
            _matchService = matchService ?? throw new ArgumentNullException( nameof(matchService));
        }

        public async Task<IActionResult> Index()
        {
            var matches = await _matchService.GetMatchesQuery()
                .OrderByDescending(m => m.StartTime)
                .ToListAsync();

            return View(matches);
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var match = await _matchService.GetMatchesQuery()
                .Include(m => m.Sets)
                    .ThenInclude(s => s.Games)
                        .ThenInclude(g => g.Points)
                            .ThenInclude(p => p.PlayedCards)
                                .ThenInclude(pc => pc.Card)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (match == null)
            {
                return NotFound();
            }

            ViewBag.Score = await _matchService.GetMatchScore(id.Value);

            return View(match);
        }
    }
}