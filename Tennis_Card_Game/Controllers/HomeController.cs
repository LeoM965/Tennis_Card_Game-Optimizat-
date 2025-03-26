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
        private readonly DbContextOptions<Tennis_Card_GameContext> _contextOptions;

        public HomeController(
            Tennis_Card_GameContext context,
            ICardService cardService,
            IPlayerService playerService,
            DbContextOptions<Tennis_Card_GameContext> contextOptions)
        {
            _context = context;
            _cardService = cardService;
            _playerService = playerService;
            _contextOptions = contextOptions;
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
            var model = new GameDashboardViewModel();

            using (var context = new Tennis_Card_GameContext(_contextOptions))
            {
                model.TopPlayers = await context.Players
                    .Include(p => p.PlayingStyle)
                    .Where(p => p.PlayingStyle != null)
                    .OrderByDescending(p => p.Level)
                    .ThenByDescending(p => p.Experience)
                    .Take(5)
                    .AsNoTracking()
                    .ToListAsync();

                model.PopularCards = await context.PlayedCards
                    .Include(pc => pc.Card)
                    .ThenInclude(c => c.CardCategory)
                    .Where(pc => pc.Card != null && pc.Card.CardCategory != null)
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
                    .AsNoTracking()
                    .ToListAsync();

                model.SurfaceStatistics = await context.Matches
                    .Include(m => m.Surface)
                    .Include(m => m.Sets)
                    .Where(m => m.IsCompleted && m.Surface != null)
                    .GroupBy(m => new { m.Surface.Id, m.Surface.Name })
                    .Select(g => new SurfaceStatistic
                    {
                        SurfaceId = g.Key.Id,
                        SurfaceName = g.Key.Name,
                        MatchCount = g.Count(),
                        AverageGamesPerSet = g.SelectMany(m => m.Sets)
                            .DefaultIfEmpty()
                            .Average(s => s != null ? s.Player1Games + s.Player2Games : 0)
                    })
                    .AsNoTracking()
                    .ToListAsync();

                model.RecentMatches = await context.Matches
                    .Include(m => m.Player1)
                    .Include(m => m.Player2)
                    .Include(m => m.Tournament)
                    .Include(m => m.Surface)
                    .Where(m => m.IsCompleted &&
                           m.Player1 != null &&
                           m.Player2 != null &&
                           m.Tournament != null &&
                           m.Surface != null)
                    .OrderByDescending(m => m.EndTime)
                    .Take(10)
                    .AsNoTracking()
                    .ToListAsync();

                model.StyleDistribution = await context.Players
                    .Include(p => p.PlayingStyle)
                    .Where(p => p.PlayingStyle != null)
                    .GroupBy(p => new { p.PlayingStyle.Id, p.PlayingStyle.Name })
                    .Select(g => new PlayingStyleDistribution
                    {
                        StyleId = g.Key.Id,
                        StyleName = g.Key.Name,
                        PlayerCount = g.Count()
                    })
                    .AsNoTracking()
                    .ToListAsync();

                model.TotalPlayers = await context.Players.AsNoTracking().CountAsync();
                model.TotalCards = await context.Cards.AsNoTracking().CountAsync();
                model.TotalMatches = await context.Matches.AsNoTracking().CountAsync();
            }

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