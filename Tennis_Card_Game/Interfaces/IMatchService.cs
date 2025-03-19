using System.Linq;
using Tennis_Card_Game.Data;
using Tennis_Card_Game.Models;

namespace Tennis_Card_Game.Services
{
    public interface IMatchService
    {
        IQueryable<Match> GetMatchesQuery();
        Task<string> GetMatchScore(int matchId);
    }
}