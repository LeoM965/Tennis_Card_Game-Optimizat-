using Microsoft.EntityFrameworkCore;
using Tennis_Card_Game.Data;
using Tennis_Card_Game.Interfaces;
using Tennis_Card_Game.Models;

namespace Tennis_Card_Game.Services
{
    public class PlayerSearchService : IPlayerService
    {
        private readonly Tennis_Card_GameContext _context;

        public PlayerSearchService(Tennis_Card_GameContext context)
        {
            _context = context;
        }

        public IEnumerable<Player> SearchPlayers(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
                return new List<Player>();

            return _context.Players
                .Include(p => p.PlayingStyle)
                .Include(p => p.SpecialAbility)
                .Where(p => p.Name.Contains(query) ||
                       p.PlayingStyle.Name.Contains(query) ||
                       p.SpecialAbility.Name.Contains(query))
                .ToList();
        }
    }

}
