using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Tennis_Card_Game.Data;
using Tennis_Card_Game.Services;

namespace Tennis_Card_Game.Controllers
{
    public class MatchListingController : Controller
    {
        private readonly IMatchService _matchService;
        public MatchListingController(IMatchService matchService) 
        {
            _matchService = matchService;
        }

        public async Task<IActionResult> LiveMatches()
        {
            var currentTime = DateTime.Now;

            var liveMatches = await _matchService.GetMatchesQuery()
                .Where(m => !m.IsCompleted && m.StartTime <= currentTime)
                .ToListAsync();

            return View(liveMatches);
        }

        public async Task<IActionResult> UpcomingMatches()
        {
            var currentTime = DateTime.Now;

            var upcomingMatches = await _matchService.GetMatchesQuery()
                .Where(m => !m.IsCompleted && m.StartTime > currentTime)
                .OrderBy(m => m.StartTime)
                .Take(10)
                .ToListAsync();

            return View(upcomingMatches);
        }

        public async Task<IActionResult> CompletedMatches()
        {
            var completedMatches = await _matchService.GetMatchesQuery()
                .Where(m => m.IsCompleted)
                .OrderByDescending(m => m.StartTime)
                .Take(20)
                .ToListAsync();

            return View(completedMatches);
        }
    }
}