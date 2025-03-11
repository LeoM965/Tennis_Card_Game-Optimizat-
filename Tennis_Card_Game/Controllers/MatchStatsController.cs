using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Tennis_Card_Game.Data;
using Tennis_Card_Game.ViewModel;

namespace Tennis_Card_Game.Controllers
{
    public class MatchStatsController : Controller
    {
        private readonly Tennis_Card_GameContext _context;

        public MatchStatsController(Tennis_Card_GameContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public async Task<IActionResult> CardUsageStats()
        {
            var cardUsageStats = await _context.PlayedCards
                .Include(pc => pc.Card)
                .Include(pc => pc.Point.Game.Set.Match)
                .GroupBy(pc => pc.CardId)
                .Select(g => new CardUsageViewModel
                {
                    CardId = g.Key,
                    CardName = g.First().Card.Name,
                    UsageCount = g.Count(),
                    WinRate = g.Count(pc => pc.Point.Game.Set.Match.IsCompleted &&
                        ((pc.PlayerId == pc.Point.Game.Set.Match.Player1Id && pc.Point.Game.Set.Match.Player1Sets > pc.Point.Game.Set.Match.Player2Sets) ||
                         (pc.PlayerId == pc.Point.Game.Set.Match.Player2Id && pc.Point.Game.Set.Match.Player2Sets > pc.Point.Game.Set.Match.Player1Sets))) * 100.0 / (g.Count() > 0 ? g.Count() : 1)
                })
                .OrderByDescending(s => s.UsageCount)
                .ToListAsync();

            return View(cardUsageStats);
        }

        public async Task<IActionResult> PopularCardsByPlayer(int playerId)
        {
            var player = await _context.Players
                .FirstOrDefaultAsync(p => p.Id == playerId);

            if (player == null)
            {
                return NotFound();
            }

            var playerCardStats = await _context.PlayedCards
                .Include(pc => pc.Card)
                .Where(pc => pc.PlayerId == playerId)
                .GroupBy(pc => pc.CardId)
                .Select(g => new PlayerCardViewModel
                {
                    CardId = g.Key,
                    CardName = g.First().Card.Name,
                    UsageCount = g.Count(),
                    EffectiveUseCount = g.Count(),
                    EffectivenessRate = 100.0
                })
                .OrderByDescending(s => s.UsageCount)
                .Take(10)
                .ToListAsync();

            ViewBag.PlayerName = player.Name;

            return View(playerCardStats);
        }
    }
}