using Tennis_Card_Game.Models;

namespace Tennis_Card_Game.ViewModel
{
    public class DeckBuilderVM
    {
        public Player Player { get; set; }
        public Dictionary<string, List<Card>> CardsByCategory { get; set; }
        public List<Card> CurrentDeckCards { get; set; }
        public List<Surface> Surfaces { get; set; }
        public int? SelectedSurfaceId { get; set; }
        public int PlayerId { get; set; }
        public List<int> SelectedCardIds { get; set; }
        public string DeckName { get; set; }
    }
}
