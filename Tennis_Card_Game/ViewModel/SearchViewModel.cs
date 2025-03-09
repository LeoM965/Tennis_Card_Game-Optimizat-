using Tennis_Card_Game.Models;

namespace Tennis_Card_Game.ViewModel
{
    public class SearchViewModel
    {
        public string Query { get; set; }
        public string SearchType { get; set; }
        public IEnumerable<Card> Cards { get; set; } = new List<Card>();
        public IEnumerable<Player> Players { get; set; } = new List<Player>();
    }
}
