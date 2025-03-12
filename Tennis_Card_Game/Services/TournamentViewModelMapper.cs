using Tennis_Card_Game.Models;
using Tennis_Card_Game.ViewModel;

namespace Tennis_Card_Game.Services
{
    public static class TournamentViewModelMapper
    {
        public static TournamentViewModel MapToViewModel(Tournament tournament) =>
            new TournamentViewModel
            {
                Id = tournament.Id,
                Name = tournament.Name,
                StartTime = tournament.StartTime,
                EndTime = tournament.EndTime,
                Surface = tournament.Surface.Name,
                Level = tournament.Level,
                XpReward = tournament.XpReward,
                CoinReward = tournament.CoinReward,
                MatchCount = tournament.Matches.Count,
                Matches = tournament.Matches.Select(m => new MatchViewModel
                {
                    Id = m.Id,
                    Player1Name = m.Player1?.Name ?? "Unknown",
                    Player2Name = m.Player2?.Name ?? "Unknown",
                    Player1Sets = m.Player1Sets,
                    Player2Sets = m.Player2Sets,
                    IsCompleted = m.IsCompleted,
                    StartTime = m.StartTime
                }).ToList()
            };
    }
}