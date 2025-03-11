using Microsoft.EntityFrameworkCore;
using Tennis_Card_Game.Data;
using Tennis_Card_Game.Models;

namespace Tennis_Card_Game.Services
{
    public class MatchService : IMatchService
    {
        private readonly Tennis_Card_GameContext _context;
        private readonly ILogger<MatchService> _logger;

        public MatchService(Tennis_Card_GameContext context, ILogger<MatchService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<(bool Success, string ErrorMessage, int TournamentId)> CompleteMatchAsync(int matchId, int player1Sets, int player2Sets)
        {
            Match? match = await _context.Matches
                .Include(m => m.Tournament)
                .FirstOrDefaultAsync(m => m.Id == matchId);

            if (match == null)
                return (false, "Match not found", 0);

            if (player1Sets < 0 || player2Sets < 0 || (player1Sets == player2Sets))
            {
                return (false, "Invalid set scores. One player must win.", match.TournamentId ?? 0);
            }

            match.Player1Sets = player1Sets;
            match.Player2Sets = player2Sets;
            match.IsCompleted = true;
            match.EndTime = DateTime.Now;

            await _context.SaveChangesAsync();
            await UpdateNextRoundMatchAsync(match);

            return (true, string.Empty, match.TournamentId ?? 0);
        }

        public async Task UpdateNextRoundMatchAsync(Match completedMatch)
        {
            int winnerId = completedMatch.Player1Sets > completedMatch.Player2Sets
                ? completedMatch.Player1Id
                : completedMatch.Player2Id;

            int nextRound = completedMatch.Round + 1;
            int nextMatchOrder = (completedMatch.MatchOrder + 1) / 2;

            Match? nextMatch = await _context.Matches
                .FirstOrDefaultAsync(m =>
                    m.TournamentId == completedMatch.TournamentId &&
                    m.Round == nextRound &&
                    m.MatchOrder == nextMatchOrder);

            if (nextMatch == null)
                return;

            if (completedMatch.MatchOrder % 2 == 1)
                nextMatch.Player1Id = winnerId;
            else
                nextMatch.Player2Id = winnerId;

            await _context.SaveChangesAsync();
        }
    }
}