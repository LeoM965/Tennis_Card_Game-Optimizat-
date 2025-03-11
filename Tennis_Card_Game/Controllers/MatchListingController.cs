using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Tennis_Card_Game.Data;

namespace Tennis_Card_Game.Controllers
{
    public class MatchListingController : MatchesController
    {
        public MatchListingController(Tennis_Card_GameContext context) : base(context)
        {
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
    }
}