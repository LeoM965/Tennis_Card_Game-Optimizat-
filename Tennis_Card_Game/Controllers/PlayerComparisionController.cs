using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Tennis_Card_Game.Data;
using Tennis_Card_Game.Services;

namespace Tennis_Card_Game.Controllers
{
    public class PlayerComparisonController : Controller
    {
        private readonly Tennis_Card_GameContext _context;
        private readonly IMatchService _matchService;

        public PlayerComparisonController(Tennis_Card_GameContext context , IMatchService matchService) 
        {
            _context = context;
            _matchService = matchService;
        }

        public async Task<IActionResult> HeadToHead(int player1Id, int player2Id)
        {
            var player1 = await _context.Players.FindAsync(player1Id);
            var player2 = await _context.Players.FindAsync(player2Id);

            if (player1 == null || player2 == null)
            {
                return NotFound();
            }

            var matches = await _matchService.GetMatchesQuery()
                .Where(m =>
                    (m.Player1Id == player1Id && m.Player2Id == player2Id) ||
                    (m.Player1Id == player2Id && m.Player2Id == player1Id))
                .OrderByDescending(m => m.StartTime)
                .ToListAsync();

            int player1Wins = matches.Count(m =>
                (m.Player1Id == player1Id && m.IsCompleted && m.Player1Sets > m.Player2Sets) ||
                (m.Player2Id == player1Id && m.IsCompleted && m.Player2Sets > m.Player1Sets));

            int player2Wins = matches.Count(m =>
                (m.Player1Id == player2Id && m.IsCompleted && m.Player1Sets > m.Player2Sets) ||
                (m.Player2Id == player2Id && m.IsCompleted && m.Player2Sets > m.Player1Sets));

            ViewBag.Player1Name = player1.Name;
            ViewBag.Player2Name = player2.Name;
            ViewBag.Player1Wins = player1Wins;
            ViewBag.Player2Wins = player2Wins;

            return View(matches);
        }
    }
}