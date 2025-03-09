namespace Tennis_Card_Game.ViewModel
{
    public class MatchViewModel
    {
        public int Id { get; set; }
        public string Player1Name { get; set; }
        public string Player2Name { get; set; }
        public int Player1Sets { get; set; }
        public int Player2Sets { get; set; }
        public bool IsCompleted { get; set; }
        public DateTime StartTime { get; set; }
    }
}
