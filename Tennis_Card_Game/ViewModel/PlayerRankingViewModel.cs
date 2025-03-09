using System;
using Tennis_Card_Game.Models;

namespace Tennis_Card_Game.ViewModel
{
    public class RankingsViewModel
    {
        public List<Player> Players { get; set; } = new List<Player>();
        public Player? CurrentPlayer { get; set; }
        public int CurrentPlayerRank { get; set; }
    }
}