using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Tennis_Card_Game.Data;
using Tennis_Card_Game.Models;
using Tennis_Card_Game.Services;

namespace Tennis_Card_Game.Controllers
{
    [Authorize]
    public class PlayerController : Controller
    {
        private readonly Tennis_Card_GameContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly PlayerService _playerService;
        private readonly TrainingService _trainingService;

        public PlayerController(Tennis_Card_GameContext context, UserManager<ApplicationUser> userManager, PlayerService playerService, TrainingService trainingService)
        {
            _context = context;
            _userManager = userManager;
            _playerService = playerService;
            _trainingService = trainingService;
        }

        [HttpGet]
        public async Task<IActionResult> CreatePlayer()
        {
            ApplicationUser? user = await _userManager.GetUserAsync(User);
            if (user.PlayerId.HasValue)
            {
                TempData["InfoMessage"] = "You already have a player associated with your account.";
                return RedirectToAction("PlayerDetails", new { id = user.PlayerId.Value });
            }
            ViewBag.PlayingStyles = await _context.PlayingStyles
                .Select(ps => new SelectListItem
                {
                    Value = ps.Id.ToString(),
                    Text = ps.Name
                })
                .ToListAsync();
            ViewBag.SpecialAbilities = await _context.SpecialAbilities
                .Select(sa => new SelectListItem
                {
                    Value = sa.Id.ToString(),
                    Text = sa.Name
                })
                .ToListAsync();
            return View(new Player
            {
                Level = 1,
                Experience = 0,
                MaxEnergy = 100,
                CurrentEnergy = 100
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreatePlayer(Player player)
        {
            ApplicationUser? user = await _userManager.GetUserAsync(User);
            if (user.PlayerId.HasValue)
            {
                TempData["InfoMessage"] = "You already have a player associated with your account.";
                return RedirectToAction("PlayerDetails", new { id = user.PlayerId.Value });
            }
            ViewBag.PlayingStyles = await _context.PlayingStyles
                .Select(ps => new SelectListItem
                {
                    Value = ps.Id.ToString(),
                    Text = ps.Name
                })
                .ToListAsync();
            ViewBag.SpecialAbilities = await _context.SpecialAbilities
                .Select(sa => new SelectListItem
                {
                    Value = sa.Id.ToString(),
                    Text = sa.Name
                })
                .ToListAsync();
            if (string.IsNullOrWhiteSpace(player.Name))
            {
                ModelState.AddModelError(nameof(player.Name), "Player name is required.");
                return View(player);
            }
            player.Name = player.Name.Trim();
            Player? existingPlayer = await _context.Players
                .FirstOrDefaultAsync(p => p.Name.ToLower() == player.Name.ToLower());
            if (existingPlayer != null)
            {
                ModelState.AddModelError(nameof(player.Name), $"A player with the name '{player.Name}' already exists.");
                return View(player);
            }
            bool playingStyleExists = await _context.PlayingStyles
                .AnyAsync(ps => ps.Id == player.PlayingStyleId);
            bool specialAbilityExists = await _context.SpecialAbilities
                .AnyAsync(sa => sa.Id == player.SpecialAbilityId);
            if (!playingStyleExists)
            {
                ModelState.AddModelError(nameof(player.PlayingStyleId), "Selected Playing Style is invalid.");
                return View(player);
            }
            if (!specialAbilityExists)
            {
                ModelState.AddModelError(nameof(player.SpecialAbilityId), "Selected Special Ability is invalid.");
                return View(player);
            }
            player.Level = 1;
            player.Experience = 0;
            player.MaxEnergy = 100;
            player.CurrentEnergy = 100;
            player.UserId = user.Id;
            _context.Players.Add(player);
            await _context.SaveChangesAsync();
            user.PlayerId = player.Id;
            await _userManager.UpdateAsync(user);
            TempData["SuccessMessage"] = $"Player {player.Name} created successfully!";
            return RedirectToAction("PlayerDetails", new { id = player.Id });
        }

        [HttpGet]
        public async Task<IActionResult> PlayerDetails(int id)
        {
            Player? player = await _context.Players
                .AsNoTracking()
                .Include(p => p.PlayingStyle)
                .Include(p => p.SpecialAbility)
                .Include(p => p.PlayerCards)
                    .ThenInclude(pc => pc.Card)
                        .ThenInclude(c => c.CardCategory)
                .Include(p => p.MatchesAsPlayer1)
                    .ThenInclude(m => m.Tournament)
                .Include(p => p.MatchesAsPlayer2)
                    .ThenInclude(m => m.Tournament)
                .FirstOrDefaultAsync(p => p.Id == id);
            if (player == null)
            {
                return NotFound();
            }
            List<Match> recentMatches = player.MatchesAsPlayer1
                .Concat(player.MatchesAsPlayer2)
                .OrderByDescending(m => m.StartTime)
                .Take(5)
                .ToList();
            List<PlayerCard> playerCards = player.PlayerCards
                .Where(pc => pc.InDeck)
                .ToList();
            int wins = player.MatchesAsPlayer1.Count(m => m.WinnerId == player.Id) +
                       player.MatchesAsPlayer2.Count(m => m.WinnerId == player.Id);
            int losses = player.MatchesAsPlayer1.Count(m => m.WinnerId != player.Id && m.WinnerId != null) +
                         player.MatchesAsPlayer2.Count(m => m.WinnerId != player.Id && m.WinnerId != null);
            List<Card> recommendedCards = await _context.Cards
                .AsNoTracking()
                .Include(c => c.CardCategory)
                .Where(c => !player.PlayerCards.Select(pc => pc.CardId).Contains(c.Id))
                .Take(4)
                .ToListAsync();
            var viewModel = new PlayerDetailsViewModel
            {
                Player = player,
                PlayerCards = playerCards,
                RecentMatches = recentMatches,
                Wins = wins,
                Losses = losses,
                RecommendedCards = recommendedCards
            };
            return View(viewModel);
        }

        [HttpGet]
        public async Task<IActionResult> PlayerList()
        {
            ApplicationUser? user = await _userManager.GetUserAsync(User);
            if (!user.PlayerId.HasValue)
            {
                return RedirectToAction("CreatePlayer");
            }
            Player? currentPlayer = await _context.Players
                .Include(p => p.PlayingStyle)
                .Include(p => p.SpecialAbility)
                .FirstOrDefaultAsync(p => p.Id == user.PlayerId.Value);
            if (currentPlayer == null)
            {
                return RedirectToAction("CreatePlayer");
            }
            List<Player> playersInRange = await _context.Players
                .Include(p => p.PlayingStyle)
                .Include(p => p.SpecialAbility)
                .Where(p => p.Id >= 66 && p.Id <= 97)
                .ToListAsync();
            List<Player> playersToShow = playersInRange.ToList();
            if (!playersToShow.Any(p => p.Id == currentPlayer.Id))
            {
                playersToShow.Add(currentPlayer);
            }
            return View(playersToShow);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int? id)
        {
            ApplicationUser? user = await _userManager.GetUserAsync(User);
            if (!user.PlayerId.HasValue)
            {
                return RedirectToAction("CreatePlayer");
            }
            if (id != user.PlayerId.Value)
            {
                TempData["ErrorMessage"] = "You can only edit your own player.";
                return RedirectToAction("PlayerDetails", new { id = user.PlayerId.Value });
            }
            Player? player = await _context.Players.FindAsync(id);
            if (player == null)
            {
                return NotFound();
            }
            ViewBag.PlayingStyles = await _context.PlayingStyles
                .Select(ps => new SelectListItem
                {
                    Value = ps.Id.ToString(),
                    Text = ps.Name,
                    Selected = ps.Id == player.PlayingStyleId
                })
                .ToListAsync();
            ViewBag.SpecialAbilities = await _context.SpecialAbilities
                .Select(sa => new SelectListItem
                {
                    Value = sa.Id.ToString(),
                    Text = sa.Name,
                    Selected = sa.Id == player.SpecialAbilityId
                })
                .ToListAsync();
            return View(player);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Player player)
        {
            ApplicationUser? user = await _userManager.GetUserAsync(User);
            if (user.PlayerId != player.Id)
            {
                TempData["ErrorMessage"] = "You can only edit your own player.";
                return RedirectToAction("PlayerDetails", new { id = user.PlayerId });
            }
            if (string.IsNullOrWhiteSpace(player.Name))
            {
                ModelState.AddModelError(nameof(player.Name), "Player name is required.");
                ViewBag.PlayingStyles = await _context.PlayingStyles
                    .Select(ps => new SelectListItem
                    {
                        Value = ps.Id.ToString(),
                        Text = ps.Name,
                        Selected = ps.Id == player.PlayingStyleId
                    })
                    .ToListAsync();
                ViewBag.SpecialAbilities = await _context.SpecialAbilities
                    .Select(sa => new SelectListItem
                    {
                        Value = sa.Id.ToString(),
                        Text = sa.Name,
                        Selected = sa.Id == player.SpecialAbilityId
                    })
                    .ToListAsync();
                return View(player);
            }
            player.Name = player.Name.Trim();
            Player? existingPlayer = await _context.Players
                .FirstOrDefaultAsync(p => p.Name.ToLower() == player.Name.ToLower() && p.Id != player.Id);
            if (existingPlayer != null)
            {
                ModelState.AddModelError(nameof(player.Name), $"A player with the name '{player.Name}' already exists.");
                ViewBag.PlayingStyles = await _context.PlayingStyles
                    .Select(ps => new SelectListItem
                    {
                        Value = ps.Id.ToString(),
                        Text = ps.Name,
                        Selected = ps.Id == player.PlayingStyleId
                    })
                    .ToListAsync();
                ViewBag.SpecialAbilities = await _context.SpecialAbilities
                    .Select(sa => new SelectListItem
                    {
                        Value = sa.Id.ToString(),
                        Text = sa.Name,
                        Selected = sa.Id == player.SpecialAbilityId
                    })
                    .ToListAsync();
                return View(player);
            }
            Player? originalPlayer = await _context.Players.FindAsync(player.Id);
            originalPlayer.Name = player.Name;
            originalPlayer.PlayingStyleId = player.PlayingStyleId;
            originalPlayer.SpecialAbilityId = player.SpecialAbilityId;
            originalPlayer.UserId = user.Id;
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = $"Player {player.Name} updated successfully!";
            return RedirectToAction("PlayerDetails", new { id = player.Id });
        }
        public async Task<IActionResult> PlayerProgression(int id)
        {
            id = await _playerService.EnsureValidPlayerId(id);
            Player? player = await _context.Players
                .AsNoTracking()
                .Include(p => p.PlayingStyle)
                .Include(p => p.SpecialAbility)
                .Include(p => p.PlayerCards)
                .FirstOrDefaultAsync(p => p.Id == id);
            if (player == null)
                return NotFound($"No player found with ID {id}");
            PlayerProgressionViewModel progressionViewModel = new PlayerProgressionViewModel
            {
                Player = player,
                CurrentLevelXpRequirement = _playerService.CalculateXpForNextLevel(player.Level),
                XpToNextLevel = _playerService.CalculateXpToNextLevel(player),
                LevelProgressPercentage = _playerService.CalculateLevelProgressPercentage(player),
                AvailableSkillPoints = _playerService.CalculateAvailableSkillPoints(player),
                SkillUpgradeCost = _playerService.CalculateSkillUpgradeCost(player),
                PossiblePlayingStyles = await _context.PlayingStyles
                    .AsNoTracking()
                    .Where(ps => ps.Id != player.PlayingStyleId)
                    .ToListAsync(),
                PossibleSpecialAbilities = await _context.SpecialAbilities
                    .AsNoTracking()
                    .Where(sa => sa.Id != player.SpecialAbilityId)
                    .ToListAsync(),
                RecommendedTrainingModules = _trainingService.GenerateRecommendedTrainingModules(player)
            };
            return View(progressionViewModel);
        }

        public async Task<IActionResult> TrainingCamp(int id)
        {
            Player? player = await _context.Players
                .Include(p => p.PlayingStyle)
                .FirstOrDefaultAsync(p => p.Id == id);
            if (player == null)
            {
                return RedirectToAction(nameof(PlayerList));
            }
            TrainingCampViewModel trainingOptions = new TrainingCampViewModel
            {
                Player = player,
                AvailableTrainingModules = _trainingService.GenerateRecommendedTrainingModules(player)
            };
            return View(trainingOptions);
        }
    }
}