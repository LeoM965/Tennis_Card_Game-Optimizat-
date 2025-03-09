using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Tennis_Card_Game.Data;
using Tennis_Card_Game.Interfaces;
using Tennis_Card_Game.ViewModel;
using Tennis_Card_Game.Models;
using Microsoft.AspNetCore.Authorization;

namespace Tennis_Card_Game.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        private readonly Tennis_Card_GameContext _context;
        private readonly ICardService _cardService;
        private readonly IPlayerService _playerService;

        public HomeController(Tennis_Card_GameContext context, ICardService cardService, IPlayerService playerService)
        {
            _context = context;
            _cardService = cardService;
            _playerService = playerService;
        }

        public async Task<IActionResult> Index()
        {
            GameIntroductionVM model = new GameIntroductionVM
            {
                BasicRules = new List<string>
                {
                    "Win 6 games with at least a 2-point difference",
                    "Manage energy to avoid fatigue",
                    "Combine cards for synergy bonuses",
                    "Use special abilities strategically"
                },
                StarterPlayers = await _context.Players
                    .Include(p => p.PlayingStyle)
                    .Where(p => p.Level <= 5)
                    .Take(3)
                    .ToListAsync(),
                EssentialCards = await _context.Cards
                    .Include(c => c.CardCategory)
                    .Where(c => c.EnergyConsumption <= 30)
                    .OrderBy(c => c.CardCategory.Name)
                    .Take(6)
                    .ToListAsync(),
                RecentTournaments = await _context.Tournaments
                    .Include(t => t.Surface)
                    .OrderByDescending(t => t.StartTime)
                    .Take(2)
                    .ToListAsync()
            };
            return View(model);
        }

        public async Task<IActionResult> GameDashboard()
        {
            List<Player> topPlayers = await _context.Players
                .Include(p => p.PlayingStyle)
                .OrderByDescending(p => p.Level)
                .ThenByDescending(p => p.Experience)
                .Take(5)
                .ToListAsync();

            List<CardStatistic> popularCards = await _context.PlayedCards
                .Include(pc => pc.Card)
                .ThenInclude(c => c.CardCategory)
                .GroupBy(pc => new { pc.Card.Id, pc.Card.Name, CategoryName = pc.Card.CardCategory.Name })
                .Select(g => new CardStatistic
                {
                    CardId = g.Key.Id,
                    CardName = g.Key.Name,
                    CategoryName = g.Key.CategoryName,
                    UsageCount = g.Count()
                })
                .OrderByDescending(c => c.UsageCount)
                .Take(5)
                .ToListAsync();

            List<SurfaceStatistic> surfaceStats = await _context.Matches
                .Include(m => m.Surface)
                .Include(m => m.Sets)
                .Where(m => m.IsCompleted)
                .GroupBy(m => new { m.Surface.Id, m.Surface.Name })
                .Select(g => new SurfaceStatistic
                {
                    SurfaceId = g.Key.Id,
                    SurfaceName = g.Key.Name,
                    MatchCount = g.Count(),
                    AverageGamesPerSet = g.SelectMany(m => m.Sets)
                                           .Average(s => s.Player1Games + s.Player2Games)
                })
                .ToListAsync();

            List<Match> recentMatches = await _context.Matches
                .Include(m => m.Player1)
                .Include(m => m.Player2)
                .Include(m => m.Tournament)
                .Include(m => m.Surface)
                .Where(m => m.IsCompleted)
                .OrderByDescending(m => m.EndTime)
                .Take(10)
                .ToListAsync();

            List<PlayingStyleDistribution> styleDistribution = await _context.Players
                .Include(p => p.PlayingStyle)
                .GroupBy(p => new { p.PlayingStyle.Id, p.PlayingStyle.Name })
                .Select(g => new PlayingStyleDistribution
                {
                    StyleId = g.Key.Id,
                    StyleName = g.Key.Name,
                    PlayerCount = g.Count()
                })
                .ToListAsync();

            GameDashboardViewModel model = new GameDashboardViewModel
            {
                TopPlayers = topPlayers,
                PopularCards = popularCards,
                SurfaceStatistics = surfaceStats,
                RecentMatches = recentMatches,
                StyleDistribution = styleDistribution,
                TotalPlayers = await _context.Players.CountAsync(),
                TotalCards = await _context.Cards.CountAsync(),
                TotalMatches = await _context.Matches.CountAsync()
            };

            return View(model);
        }

        public IActionResult Search(string query, string type = "all")
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                return RedirectToAction("Index");
            }

            SearchViewModel searchViewModel = new SearchViewModel
            {
                Query = query,
                SearchType = type
            };

            switch (type.ToLower())
            {
                case "cards":
                    searchViewModel.Cards = _cardService.SearchCards(query);
                    break;
                case "players":
                    searchViewModel.Players = _playerService.SearchPlayers(query);
                    break;
                case "all":
                default:
                    searchViewModel.Cards = _cardService.SearchCards(query);
                    searchViewModel.Players = _playerService.SearchPlayers(query);
                    break;
            }

            return View(searchViewModel);
        }
    }
}