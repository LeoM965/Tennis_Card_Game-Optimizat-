using Microsoft.EntityFrameworkCore;
using System.Linq;
using Tennis_Card_Game.Data;
using Tennis_Card_Game.Models;

namespace Tennis_Card_Game.Services
{
    public class MatchService : IMatchService
    {
        private readonly Tennis_Card_GameContext _context;

        public MatchService(Tennis_Card_GameContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public IQueryable<Match> GetMatchesQuery()
        {
            return _context.Matches
                .Include(m => m.Player1)
                .Include(m => m.Player2)
                .Include(m => m.Surface)
                .Include(m => m.Tournament)
                .Include(m => m.WeatherCondition);
        }
        public async Task<string> GetMatchScore(int matchId)
        {
            var match = await _context.Matches
                .Include(m => m.Sets)
                .FirstOrDefaultAsync(m => m.Id == matchId);

            if (match == null || match.Sets == null || !match.Sets.Any())
            {
                return "Score not available";
            }

            var scoreBuilder = new System.Text.StringBuilder();

            foreach (var set in match.Sets.OrderBy(s => s.SetNumber))
            {
                scoreBuilder.Append($"{set.Player1Games}-{set.Player2Games} ");
            }

            return scoreBuilder.ToString().Trim();
        }
    }
}