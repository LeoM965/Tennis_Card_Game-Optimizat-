using Tennis_Card_Game.Models;

namespace Tennis_Card_Game.ViewModel
{
    public class BrowseCardsVM
    {
        public List<Card> Cards { get; set; }
        public List<string> Categories { get; set; }
        public string SelectedCategory { get; set; }
        public List<string> SubCategories { get; set; }
        public string SelectedSubCategory { get; set; }
        public List<Surface> Surfaces { get; set; }
        public string SelectedSurface { get; set; }
    }
}