using Tennis_Card_Game.Models;

namespace Tennis_Card_Game.Interfaces
{
    public interface ICardService
    {
        IEnumerable<Card> SearchCards(string query);
    }
}
