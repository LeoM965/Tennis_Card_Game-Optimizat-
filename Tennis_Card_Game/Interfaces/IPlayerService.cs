using Tennis_Card_Game.Models;

namespace Tennis_Card_Game.Interfaces
{
    public interface IPlayerService
    {
        IEnumerable<Player> SearchPlayers(string query);
    }
}
