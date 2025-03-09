using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Tennis_Card_Game.Data;
using Tennis_Card_Game.Models;
using Tennis_Card_Game.ViewModel;

namespace Tennis_Card_Game.Controllers
{
    public class CardsController : Controller
    {
        private readonly Tennis_Card_GameContext _context;

        public CardsController(Tennis_Card_GameContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Browse(string? name = null, string? subCategory = null, string? surface = null)
        {
            IQueryable<Card> query = _context.Cards
                .Include(c => c.CardCategory)
                .AsQueryable();

            if (!string.IsNullOrEmpty(name))
            {
                query = query.Where(c => c.CardCategory.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrEmpty(subCategory))
            {
                query = query.Where(c => c.CardCategory.SubCategory.Equals(subCategory, StringComparison.OrdinalIgnoreCase));
            }

            List<string> categories = await _context.CardCategories
                .Select(c => c.Name)
                .Distinct()
                .ToListAsync() ?? new List<string>();

            List<string> subcategories = await _context.CardCategories
                .Select(c => c.SubCategory)
                .Distinct()
                .ToListAsync() ?? new List<string>();

            List<Surface> surfaces = await _context.Surfaces.ToListAsync() ?? new List<Surface>();

            BrowseCardsVM model = new BrowseCardsVM
            {
                Cards = await query.ToListAsync(),
                Categories = categories,
                SelectedCategory = name ?? string.Empty,
                SubCategories = subcategories,
                SelectedSubCategory = subCategory ?? string.Empty,
                Surfaces = surfaces,
                SelectedSurface = surface ?? string.Empty
            };

            return View(model);
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            Card? card = await _context.Cards
                .Include(c => c.CardCategory)
                .Include(c => c.SynergiesAsCard1)
                    .ThenInclude(s => s.Card2)
                .Include(c => c.SynergiesAsCard2)
                    .ThenInclude(s => s.Card1)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (card == null)
            {
                return NotFound();
            }

            return View(card);
        }

        public async Task<IActionResult> DeckBuilder(int? Id)
        {
            if (!Id.HasValue)
            {
                return RedirectToAction("Index", "Players");
            }
            try
            {
                Player? player = await _context.Players
                    .Include(p => p.PlayerCards)
                        .ThenInclude(pc => pc.Card)
                            .ThenInclude(c => c.CardCategory)
                    .FirstOrDefaultAsync(p => p.Id == Id);
                if (player == null)
                {
                    return NotFound();
                }

                bool isRestrictedPlayer = player.Id >= 66 && player.Id <= 97;

                Dictionary<string, List<Card>> cardsGroupedByCategory;
                if (!isRestrictedPlayer)
                {
                    cardsGroupedByCategory = await _context.Cards
                        .Include(c => c.CardCategory)
                        .GroupBy(c => c.CardCategory.Name)
                        .ToDictionaryAsync(
                            g => g.Key,
                            g => g.ToList()
                        );
                }
                else
                {
                    cardsGroupedByCategory = new Dictionary<string, List<Card>>();
                }

                List<Card> currentDeckCards = player.PlayerCards
                    .Where(pc => pc.InDeck)
                    .Select(pc => pc.Card)
                    .ToList();
                List<Surface> surfaces = await _context.Surfaces.ToListAsync();

                DeckBuilderVM model = new DeckBuilderVM
                {
                    Player = player,
                    CardsByCategory = cardsGroupedByCategory,
                    CurrentDeckCards = currentDeckCards,
                    Surfaces = surfaces,
                    PlayerId = player.Id,
                    DeckName = $"{player.Name}'s Deck"
                };

                if (isRestrictedPlayer)
                {
                    TempData["RestrictedPlayer"] = "This player cannot modify it here.";
                }

                return View(model);
            }
            catch (System.Exception ex)
            {
                throw;
            }
        }

        public async Task<IActionResult> SaveDeck(DeckBuilderVM model)
        {
            if (model.PlayerId <= 0)
            {
                return RedirectToAction("Index", "Players");
            }

            try
            {
                Player? player = await _context.Players
                    .Include(p => p.PlayerCards)
                        .ThenInclude(pc => pc.Card)
                    .FirstOrDefaultAsync(p => p.Id == model.PlayerId);

                if (player == null)
                {
                    return NotFound();
                }
                        
                if (player.Id >= 66 && player.Id <= 97)
                {
                    TempData["Error"] = "Players must bring their own deck and cannot modify it here.";
                    return RedirectToAction("PlayerDetails", "Players", new { id = player.Id });
                }

                foreach (PlayerCard playerCard in player.PlayerCards)
                {
                    if (playerCard.InDeck)
                    {
                        playerCard.InDeck = false;
                    }
                }

                if (model.SelectedCardIds != null && model.SelectedCardIds.Any())
                {
                    List<int> selectedCardIds = model.SelectedCardIds.ToList();
                    List<Card> cardsToProcess = await _context.Cards
                        .Include(c => c.CardCategory)
                        .Where(c => selectedCardIds.Contains(c.Id))
                        .ToListAsync();

                    foreach (int cardId in selectedCardIds)
                    {
                        PlayerCard? playerCard = player.PlayerCards.FirstOrDefault(pc => pc.CardId == cardId);
                        if (playerCard != null)
                        {
                            playerCard.InDeck = true;
                        }
                        else
                        {
                            Card? card = cardsToProcess.FirstOrDefault(c => c.Id == cardId);
                            if (card != null)
                            {
                                player.PlayerCards.Add(new PlayerCard
                                {
                                    PlayerId = player.Id,
                                    CardId = cardId,
                                    Card = card,
                                    InDeck = true
                                });
                            }
                        }
                    }
                }

                await _context.SaveChangesAsync();
                TempData["Success"] = "Deck saved successfully!";
                return RedirectToAction("PlayerDetails", "Players", new { id = player.Id });
            }
            catch (System.Exception ex)
            {
                throw;
            }
        }
    }
}