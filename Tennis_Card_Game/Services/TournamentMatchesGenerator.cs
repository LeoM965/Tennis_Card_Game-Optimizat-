using Microsoft.EntityFrameworkCore;
using Tennis_Card_Game.Data;
using Tennis_Card_Game.Interfaces;
using Tennis_Card_Game.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

public class TournamentMatchesGenerator : ITournamentMatchesGenerator
{
    private readonly Tennis_Card_GameContext _context;

    public TournamentMatchesGenerator(Tennis_Card_GameContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task AddPlayerToTournamentAsync(Tournament tournament, Player player, Surface surface)
    {
        var matchWithAI = await _context.Matches
            .Include(m => m.Player1)
            .Include(m => m.Player2)
            .Where(m => m.TournamentId == tournament.Id && m.Round == 1 && !m.IsCompleted)
            .FirstOrDefaultAsync(m => m.Player1.UserId == null || m.Player2.UserId == null);

        if (matchWithAI != null)
        {
            if (matchWithAI.Player1.UserId == null)
                matchWithAI.Player1Id = player.Id;
            else
                matchWithAI.Player2Id = player.Id;

            await _context.SaveChangesAsync();
        }
        else
            throw new InvalidOperationException("No available spots in the tournament.");
    }

    public async Task GenerateTournamentMatchesAsync(Tournament tournament, Player firstPlayer, Surface surface)
    {
        int registeredPlayersCount = await _context.TournamentRegistrations
            .CountAsync(r => r.TournamentId == tournament.Id);
        int aiPlayersNeeded = Math.Max(0, 16 - registeredPlayersCount);
        List<Player> aiOpponents = await _context.Players
            .Where(p => p.UserId == null)
            .Include(p => p.PlayingStyle)
            .OrderBy(p => Guid.NewGuid())
            .Take(aiPlayersNeeded)
            .ToListAsync();
        if (aiOpponents.Count < aiPlayersNeeded)
            throw new InvalidOperationException($"There are not enough AI players in the database. Need {aiPlayersNeeded} but only found {aiOpponents.Count}");

        Player? tbdPlayer = await _context.Players.FirstOrDefaultAsync(p => p.Name == "Player");
        if (tbdPlayer == null)
        {
            tbdPlayer = new Player
            {
                Name = "PlayerTBD",
                PlayingStyleId = aiOpponents.FirstOrDefault()?.PlayingStyleId ?? 1,
                UserId = null
            };
            _context.Players.Add(tbdPlayer);
            await _context.SaveChangesAsync();
        }

        var matches = new List<Match>();
        DateTime startDateTime = DateTime.Today.Add(tournament.StartTime);
        DateTime endDateTime = tournament.EndTime.TotalHours == 0
            ? DateTime.Today.AddDays(1)
            : DateTime.Today.Add(tournament.EndTime);
        TimeSpan tournamentDuration = endDateTime - startDateTime;

        var registeredPlayers = await _context.TournamentRegistrations
            .Where(r => r.TournamentId == tournament.Id)
            .Select(r => r.Player)
            .ToListAsync();

        List<Player> allPlayers = new List<Player>(registeredPlayers);
        allPlayers.AddRange(aiOpponents);
        var shuffledPlayers = allPlayers.OrderBy(p => Guid.NewGuid()).ToList();

        for (int i = 0; i < 8; i++)
        {
            int player1Index = i * 2;
            int player2Index = i * 2 + 1;
            if (player2Index < shuffledPlayers.Count)
            {
                matches.Add(CreateMatch(
                    tournament.Id,
                    shuffledPlayers[player1Index].Id,
                    shuffledPlayers[player2Index].Id,
                    surface.Id,
                    1,
                    i + 1
                ));
            }
        }

        for (int i = 0; i < 4; i++)
            matches.Add(CreateMatch(tournament.Id, tbdPlayer.Id, tbdPlayer.Id, surface.Id, 2, i + 1));

        for (int i = 0; i < 2; i++)
            matches.Add(CreateMatch(tournament.Id, tbdPlayer.Id, tbdPlayer.Id, surface.Id, 3, i + 1));

        matches.Add(CreateMatch(tournament.Id, tbdPlayer.Id, tbdPlayer.Id, surface.Id, 4, 1));

        int totalMatches = matches.Count;

        TimeSpan matchDuration = new TimeSpan(tournamentDuration.Ticks / totalMatches);

        for (int i = 0; i < totalMatches; i++)
        {
            DateTime currentMatchStart = startDateTime.Add(new TimeSpan(matchDuration.Ticks * i));
            DateTime currentMatchEnd = i < totalMatches - 1
                ? startDateTime.Add(new TimeSpan(matchDuration.Ticks * (i + 1)))
                : endDateTime;

            matches[i].StartTime = currentMatchStart;
            matches[i].EndTime = currentMatchEnd;
        }

        HashSet<int> playerIds = matches
            .SelectMany(m => new[] { m.Player1Id, m.Player2Id })
            .Where(id => id != tbdPlayer.Id)
            .ToHashSet();

        int existingPlayerCount = await _context.Players
            .CountAsync(p => playerIds.Contains(p.Id));

        if (existingPlayerCount != playerIds.Count)
            throw new InvalidOperationException("One or more players not found in database");

        await _context.Matches.AddRangeAsync(matches);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateNextRoundMatchesAsync(int tournamentId)
    {
        var completedMatches = await _context.Matches
            .Where(m => m.TournamentId == tournamentId && m.IsCompleted && m.Round < 4)
            .ToListAsync();

        foreach (var match in completedMatches)
        {
            int nextRound = match.Round + 1;
            int matchOrder = (match.MatchOrder + 1) / 2;

            var nextRoundMatch = await _context.Matches
                .FirstOrDefaultAsync(m => m.TournamentId == tournamentId && m.Round == nextRound && m.MatchOrder == matchOrder);

            if (nextRoundMatch != null)
            {
                int winnerId = match.Player1Sets > match.Player2Sets ? match.Player1Id : match.Player2Id;

                if (match.MatchOrder % 2 == 1)
                    nextRoundMatch.Player1Id = winnerId;
                else
                    nextRoundMatch.Player2Id = winnerId;

                _context.Matches.Update(nextRoundMatch);
            }
        }

        await _context.SaveChangesAsync();
    }

    private Match CreateMatch(int tournamentId, int player1Id, int player2Id, int surfaceId, int round, int matchOrder) =>
        new Match
        {
            TournamentId = tournamentId,
            Player1Id = player1Id,
            Player2Id = player2Id,
            SurfaceId = surfaceId,
            Round = round,
            MatchOrder = matchOrder,
            IsCompleted = false,
            Player1Sets = 0,
            Player2Sets = 0
        };
}