using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Tennis_Card_Game.Data;
using Tennis_Card_Game.Models;
using Tennis_Card_Game.Services;
using Tennis_Card_Game.ViewModel;

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
                .Include(p => p.MatchesAsPlayer1)
                    .ThenInclude(m => m.Player2) 
                .Include(p => p.MatchesAsPlayer2)
                    .ThenInclude(m => m.Player1) 
                .FirstOrDefaultAsync(p => p.Id == id);

            if (player == null)
            {
                return NotFound($"No player found with ID {id}");
            }

            List<Match> recentMatches = player.MatchesAsPlayer1
                .Concat(player.MatchesAsPlayer2)
                .OrderByDescending(m => m.StartTime)
                .Take(5)
                .ToList();

            int wins = player.MatchesAsPlayer1.Count(m => m.Player1Sets > m.Player2Sets && m.IsCompleted) +
                       player.MatchesAsPlayer2.Count(m => m.Player2Sets > m.Player1Sets && m.IsCompleted);

            int losses = player.MatchesAsPlayer1.Count(m => m.Player1Sets < m.Player2Sets && m.IsCompleted) +
                         player.MatchesAsPlayer2.Count(m => m.Player2Sets < m.Player1Sets && m.IsCompleted);

            List<Card> recommendedCards = await _context.Cards
                .AsNoTracking()
                .Include(c => c.CardCategory)
                .Where(c => !player.PlayerCards.Select(pc => pc.CardId).Contains(c.Id))
                .Take(4)
                .ToListAsync();

            var viewModel = new PlayerDetailsViewModel
            {
                Player = player,
                PlayerCards = player.PlayerCards.Where(pc => pc.InDeck).ToList(),
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
            {
                return NotFound($"No player found with ID {id}");
            }

            PlayerProgressionViewModel progressionViewModel = new PlayerProgressionViewModel
            {
                Player = player,
                CurrentLevelXpRequirement = _playerService.CalculateXpForLevel(player.Level + 1),
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
                .Include(p => p.SpecialAbility)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (player == null)
            {
                TempData["ErrorMessage"] = "Player not found.";
                return RedirectToAction("Index", "Home");
            }

            var availableModules = _trainingService.GenerateRecommendedTrainingModules(player);
            double trainingBonus = _trainingService.CalculateBonusMultiplier(player);

            var viewModel = new TrainingCampViewModel
            {
                Player = player,
                AvailableTrainingModules = availableModules,
                TrainingBonus = trainingBonus,
                TrainingService = _trainingService
            };

            return View(viewModel);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PerformTraining(int playerId, string trainingModuleName)
        {
            Player? player = await _context.Players
                .Include(p => p.PlayingStyle)
                .Include(p => p.SpecialAbility)
                .FirstOrDefaultAsync(p => p.Id == playerId);
            if (player == null)
            {
                TempData["ErrorMessage"] = "Player not found.";
                return RedirectToAction("Index", "Home");
            }

            List<TrainingModule> availableModules = _trainingService.GenerateRecommendedTrainingModules(player);
            TrainingModule? selectedModule = availableModules.FirstOrDefault(m => m.Name == trainingModuleName);
            if (selectedModule == null)
            {
                TempData["ErrorMessage"] = "Training module not found.";
                return RedirectToAction("TrainingCamp", new { id = playerId });
            }

            if (player.CurrentEnergy < selectedModule.EnergyRequired)
            {
                TempData["ErrorMessage"] = "Not enough energy to complete this training.";
                return RedirectToAction("TrainingCamp", new { id = playerId });
            }
            try
            {
                player.CurrentEnergy -= selectedModule.EnergyRequired;

                int xpReward = _trainingService.CalculateTrainingXp(player, selectedModule.ExperienceReward);

                player.Experience += xpReward;

                _playerService.LevelUpPlayer(player);

                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"Training completed successfully! Gained {xpReward} XP.";

                return RedirectToAction("PlayerProgression", new { id = playerId });
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error during training: {ex.Message}";
                return RedirectToAction("TrainingCamp", new { id = playerId });
            }
        }
        [HttpGet]
        public async Task<IActionResult> Rankings()
        {
            ApplicationUser? user = await _userManager.GetUserAsync(User);
            if (!user.PlayerId.HasValue)
            {
                return RedirectToAction("CreatePlayer");
            }

            List<Player> rankedPlayers = await _context.Players
                .Include(p => p.PlayingStyle)
                .Include(p => p.SpecialAbility)
                .OrderByDescending(p => p.Level)
                .ThenByDescending(p => p.Experience)
                .ToListAsync();

            Player? currentPlayer = rankedPlayers.FirstOrDefault(p => p.Id == user.PlayerId.Value);
            int currentPlayerRank = currentPlayer != null ? rankedPlayers.IndexOf(currentPlayer) + 1 : 0;

            var viewModel = new RankingsViewModel
            {
                Players = rankedPlayers,
                CurrentPlayer = currentPlayer,
                CurrentPlayerRank = currentPlayerRank
            };

            return View(viewModel);
        }
    }
}