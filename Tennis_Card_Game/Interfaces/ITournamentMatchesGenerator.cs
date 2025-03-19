using Tennis_Card_Game.Models;

namespace Tennis_Card_Game.Interfaces
{
    public interface ITournamentMatchesGenerator
    {
        Task AddPlayerToTournamentAsync(Tournament tournament, Player player, Surface surface);
        Task GenerateTournamentMatchesAsync(Tournament tournament, Player firstPlayer, Surface surface);

        Task UpdateNextRoundMatchesAsync(int tournamentId);
    }

}