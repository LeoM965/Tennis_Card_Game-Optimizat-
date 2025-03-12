using Tennis_Card_Game.Models;

namespace Tennis_Card_Game.Interfaces
{
    public interface ITournamentEligibilityChecker
    {
        Task<(bool isEligible, string errorMessage, Player? player, Tournament? tournament)> CheckEligibilityAsync(int tournamentId, string? userId);
    }

}