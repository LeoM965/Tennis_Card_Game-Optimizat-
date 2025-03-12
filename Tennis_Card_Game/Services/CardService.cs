using Microsoft.EntityFrameworkCore;
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
                .Include(c => c.CardCategory)
                .Where(c => c.Name.Contains(query) ||
                       c.Description.Contains(query))
                .ToList();
        }

        public async Task<List<Card>> GetAllCardsAsync()
        {
            return await _context.Cards
                .Include(c => c.CardCategory)
                .ToListAsync();
        }

        public async Task<Card?> GetCardByIdAsync(int id)
        {
            return await _context.Cards
                .Include(c => c.CardCategory)
                .FirstOrDefaultAsync(c => c.Id == id);
        }

        public async Task<List<string>> GetAllCategoriesAsync()
        {
            return await _context.CardCategories
                .Select(c => c.Name)
                .Distinct()
                .ToListAsync() ?? new List<string>();
        }

        public async Task<List<string>> GetAllSubCategoriesAsync()
        {
            return await _context.CardCategories
                .Select(c => c.SubCategory)
                .Distinct()
                .ToListAsync() ?? new List<string>();
        }

        public async Task<Dictionary<string, List<Card>>> GetCardsGroupedByCategoryAsync()
        {
            return await _context.Cards
                .Include(c => c.CardCategory)
                .GroupBy(c => c.CardCategory.Name)
                .ToDictionaryAsync(
                    g => g.Key,
                    g => g.ToList()
                );
        }

        public async Task<Card?> GetCardWithSynergiesAsync(int id)
        {
            return await _context.Cards
                .Include(c => c.CardCategory)
                .Include(c => c.SynergiesAsCard1)
                    .ThenInclude(s => s.Card2)
                .Include(c => c.SynergiesAsCard2)
                    .ThenInclude(s => s.Card1)
                .FirstOrDefaultAsync(c => c.Id == id);
        }
    }
}