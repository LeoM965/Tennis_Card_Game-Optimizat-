namespace Tennis_Card_Game.ViewModel
{
    internal class TournamentJoinViewModel
    {
        public int TournamentId { get; set; }
        public string TournamentName { get; set; }
        public string Surface { get; set; }
        public TimeSpan StartTime { get; set; }
        public string Level { get; set; }
        public int XpReward { get; set; }
        public int CoinReward { get; set; }
    }
}