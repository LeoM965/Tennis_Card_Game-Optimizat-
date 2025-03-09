namespace Tennis_Card_Game.ViewModel
{
    public class CardUsageViewModel
    {
        public int CardId { get; set; }
        public string CardName { get; set; } = string.Empty;
        public int UsageCount { get; set; }
        public double WinRate { get; set; }
    }
}
