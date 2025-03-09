namespace Tennis_Card_Game.ViewModel
{
    public class TournamentViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public string Surface { get; set; }
        public string Level { get; set; }
        public int XpReward { get; set; }
        public int CoinReward { get; set; }
        public int MatchCount { get; set; }
        public List<MatchViewModel> Matches { get; set; } = new List<MatchViewModel>();
    }
}
