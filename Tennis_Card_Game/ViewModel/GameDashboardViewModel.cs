using Tennis_Card_Game.Models;

public class GameDashboardViewModel
{
    public List<Player> TopPlayers { get; set; }
    public List<CardStatistic> PopularCards { get; set; }
    public List<SurfaceStatistic> SurfaceStatistics { get; set; }
    public List<Match> RecentMatches { get; set; }
    public List<PlayingStyleDistribution> StyleDistribution { get; set; }
    public int TotalPlayers { get; set; }
    public int TotalCards { get; set; }
    public int TotalMatches { get; set; }
}

public class CardStatistic
{
    public int CardId { get; set; }
    public string CardName { get; set; }
    public string CategoryName { get; set; }
    public int UsageCount { get; set; }
}

public class SurfaceStatistic
{
    public int SurfaceId { get; set; }
    public string SurfaceName { get; set; }
    public int MatchCount { get; set; }
    public double AverageGamesPerSet { get; set; }
}

public class PlayingStyleDistribution
{
    public int StyleId { get; set; }
    public string StyleName { get; set; }
    public int PlayerCount { get; set; }
}