using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Tennis_Card_Game.Data;
using Tennis_Card_Game.ViewModel;

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

        public async Task<IActionResult> LiveMatches()
        {
            var currentTime = DateTime.Now;

            var liveMatches = await GetMatchesQuery()
                .Where(m => !m.IsCompleted && m.StartTime <= currentTime)
                .ToListAsync();

            return View(liveMatches);
        }

        public async Task<IActionResult> UpcomingMatches()
        {
            var currentTime = DateTime.Now;

            var upcomingMatches = await GetMatchesQuery()
                .Where(m => !m.IsCompleted && m.StartTime > currentTime)
                .OrderBy(m => m.StartTime)
                .Take(10)
                .ToListAsync();

            return View(upcomingMatches);
        }

        public async Task<IActionResult> CompletedMatches()
        {
            var completedMatches = await GetMatchesQuery()
                .Where(m => m.IsCompleted)
                .OrderByDescending(m => m.StartTime)
                .Take(20)
                .ToListAsync();

            return View(completedMatches);
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

            var matches = await GetMatchesQuery()
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

            var matches = await GetMatchesQuery()
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

            var matches = await GetMatchesQuery()
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

            var matches = await GetMatchesQuery()
                .Where(m => m.WeatherConditionId == weatherId)
                .OrderByDescending(m => m.StartTime)
                .ToListAsync();

            ViewBag.WeatherName = weather.Name;

            return View(matches);
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

        public async Task<IActionResult> HeadToHead(int player1Id, int player2Id)
        {
            var player1 = await _context.Players.FindAsync(player1Id);
            var player2 = await _context.Players.FindAsync(player2Id);

            if (player1 == null || player2 == null)
            {
                return NotFound();
            }

            var matches = await GetMatchesQuery()
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

        private IQueryable<Models.Match> GetMatchesQuery()
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