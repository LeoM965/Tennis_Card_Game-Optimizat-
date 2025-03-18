using Tennis_Card_Game.Models;
using Tennis_Card_Game.ViewModel;

namespace Tennis_Card_Game.Services
{
    public interface ITournamentService
    {
        Task<List<TournamentViewModel>> GetAllTournamentsAsync();
        Task<TournamentViewModel?> GetTournamentDetailsAsync(int tournamentId);
        Task<List<TournamentViewModel>> GetCurrentTournamentsAsync();
        Task<(bool isEligible, string errorMessage, Player? player, Tournament? tournament)> CheckTournamentEligibilityAsync(int tournamentId, string? userId);
        Task<(bool Success, string ErrorMessage)> JoinTournamentAsync(int tournamentId, string userId);
    }
}