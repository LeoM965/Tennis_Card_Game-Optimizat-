namespace Tennis_Card_Game.ViewModel
{
    public class PlayerCardViewModel
    {
        public int CardId { get; set; }
        public string CardName { get; set; } = string.Empty;
        public int UsageCount { get; set; }
        public int EffectiveUseCount { get; set; }
        public double EffectivenessRate { get; set; }
    }
}
