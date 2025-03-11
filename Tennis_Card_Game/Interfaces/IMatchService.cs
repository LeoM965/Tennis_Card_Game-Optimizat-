using Tennis_Card_Game.Models;

namespace Tennis_Card_Game.Services
{
    public interface IMatchService
    {
        Task<(bool Success, string ErrorMessage, int TournamentId)> CompleteMatchAsync(int matchId, int player1Sets, int player2Sets);
        Task UpdateNextRoundMatchAsync(Match completedMatch);
    }
}