using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Tennis_Card_Game.Data;

namespace Tennis_Card_Game.Controllers
{
    public class MatchesController : Controller
    {
        private readonly Tennis_Card_GameContext _context;

        public MatchesController(Tennis_Card_GameContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public async Task<IActionResult> Index()
        {
            var matches = await GetMatchesQuery()
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

            var match = await GetMatchesQuery()
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

            return View(match);
        }

        protected IQueryable<Models.Match> GetMatchesQuery()
        {
            return _context.Matches
                .Include(m => m.Player1)
                .Include(m => m.Player2)
                .Include(m => m.Surface)
                .Include(m => m.Tournament)
                .Include(m => m.WeatherCondition);
        }
    }
}