using Tennis_Card_Game.Models;

public class GameIntroductionVM
{
    public List<string> BasicRules { get; set; }
    public List<Player> StarterPlayers { get; set; }
    public List<Card> EssentialCards { get; set; }
    public List<Tournament> RecentTournaments { get; set; }
}