using Tennis_Card_Game.Models;

namespace Tennis_Card_Game.Interfaces
{
    public interface ICardService
    {
        IEnumerable<Card> SearchCards(string query);
        Task<List<Card>> GetAllCardsAsync();
        Task<Card?> GetCardByIdAsync(int id);
        Task<List<string>> GetAllCategoriesAsync();
        Task<List<string>> GetAllSubCategoriesAsync();
        Task<Dictionary<string, List<Card>>> GetCardsGroupedByCategoryAsync();
        Task<Card?> GetCardWithSynergiesAsync(int id);
    }
}