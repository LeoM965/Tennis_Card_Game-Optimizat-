using Microsoft.EntityFrameworkCore;
using Tennis_Card_Game.Data;
using Tennis_Card_Game.Interfaces;
using Tennis_Card_Game.Models;

public class TournamentEligibilityChecker : ITournamentEligibilityChecker
{
    private readonly Tennis_Card_GameContext _context;
    private readonly ILogger<TournamentEligibilityChecker> _logger;

    public TournamentEligibilityChecker(
        Tennis_Card_GameContext context,
        ILogger<TournamentEligibilityChecker> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<(bool isEligible, string errorMessage, Player? player, Tournament? tournament)> CheckEligibilityAsync(int tournamentId, string? userId)
    {
        if (string.IsNullOrEmpty(userId))
            return (false, "You must be logged in to join a tournament.", null, null);

        Tournament? tournament = await _context.Tournaments
            .AsNoTracking()
            .Include(t => t.Surface)
            .FirstOrDefaultAsync(t => t.Id == tournamentId);

        if (tournament == null)
            return (false, "Tournament not found.", null, null);

        Player? userPlayer = await _context.Players
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.UserId == userId);

        if (userPlayer == null)
            return (false, "You don't have a player to participate in the tournament!", null, tournament);

        DateTime currentTime = DateTime.Now;
        DateTime tournamentStartDateTime = DateTime.Today.Add(tournament.StartTime);
        DateTime registrationOpenTime = tournamentStartDateTime.AddHours(-1);

        if (currentTime < registrationOpenTime)
            return (false, "Registration is not open yet. You can register 1 hour before the tournament starts.",
                userPlayer, tournament);

        DateTime tournamentEndDateTime = tournament.EndTime.TotalHours == 0
            ? DateTime.Today.AddDays(1)
            : DateTime.Today.Add(tournament.EndTime);
        DateTime registrationCloseTime = tournamentEndDateTime.AddMinutes(-5);

        if (currentTime > registrationCloseTime)
            return (false, "Registration is closed. The tournament is about to end.",
                userPlayer, tournament);

        bool alreadyParticipating = await _context.Matches
            .AsNoTracking()
            .Where(m => m.TournamentId == tournamentId && !m.IsCompleted)
            .AnyAsync(m => m.Player1Id == userPlayer.Id || m.Player2Id == userPlayer.Id);

        if (alreadyParticipating)
            return (false, "You are already participating in this tournament!", userPlayer, tournament);

        bool alreadyRegistered = await _context.TournamentRegistrations
            .AsNoTracking()
            .AnyAsync(r => r.TournamentId == tournamentId && r.PlayerId == userPlayer.Id);

        if (alreadyRegistered)
            return (false, "You are already registered in this tournament!", userPlayer, tournament);

        bool tournamentStarted = await _context.Matches.AnyAsync(m => m.TournamentId == tournamentId);
        bool availableSpots = await _context.Matches
            .Where(m => m.TournamentId == tournamentId && m.Round == 1 && !m.IsCompleted)
            .Include(m => m.Player1)
            .Include(m => m.Player2)
            .AnyAsync(m => m.Player1.UserId == null || m.Player2.UserId == null);

        if (tournamentStarted && !availableSpots)
            return (false, "No available spots in this tournament.", userPlayer, tournament);

        return (true, string.Empty, userPlayer, tournament);
    }
}