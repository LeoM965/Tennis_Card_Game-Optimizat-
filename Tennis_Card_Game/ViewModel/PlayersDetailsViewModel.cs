using Tennis_Card_Game.Models;

public class PlayerDetailsViewModel
{
    public Player Player { get; set; }
    public List<PlayerCard> PlayerCards { get; set; }
    public List<Match> RecentMatches { get; set; }
    public List<Card> RecommendedCards { get; set; }
    public int Wins { get; set; }
    public int Losses { get; set; }

    public string WinLossRecord => $"{Wins}-{Losses}";
    public double WinPercentage => Wins + Losses > 0 ? (double)Wins / (Wins + Losses) * 100 : 0;
    public string EnergyStatus => $"{Player.CurrentEnergy}/{Player.MaxEnergy}";
    public int ActiveCardCount => PlayerCards.Count;
}