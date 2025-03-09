using Tennis_Card_Game.Data;
using Tennis_Card_Game.Interfaces;
using Tennis_Card_Game.Models;

namespace Tennis_Card_Game.Services
{
    public class CardService : ICardService
    {
        private readonly Tennis_Card_GameContext _context;

        public CardService(Tennis_Card_GameContext context)
        {
            _context = context;
        }

        public IEnumerable<Card> SearchCards(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
                return new List<Card>();

            return _context.Cards
                .Where(c => c.Name.Contains(query) ||
                       c.Description.Contains(query))
                .ToList();
        }
    }
}
