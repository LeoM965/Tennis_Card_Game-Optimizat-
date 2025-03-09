using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Tennis_Card_Game.Models;

namespace Tennis_Card_Game.Data
{
    public class Tennis_Card_GameContext : IdentityDbContext<ApplicationUser>
    {
        public Tennis_Card_GameContext(DbContextOptions<Tennis_Card_GameContext> options)
            : base(options)
        {
        }

        public DbSet<Player> Players { get; set; }
        public DbSet<Card> Cards { get; set; }
        public DbSet<CardCategory> CardCategories { get; set; }
        public DbSet<PlayerCard> PlayerCards { get; set; }
        public DbSet<CardSynergy> CardSynergies { get; set; }
        public DbSet<PlayingStyle> PlayingStyles { get; set; }
        public DbSet<SpecialAbility> SpecialAbilities { get; set; }
        public DbSet<Match> Matches { get; set; }
        public DbSet<Set> Sets { get; set; }
        public DbSet<Game> Games { get; set; }
        public DbSet<Point> Points { get; set; }
        public DbSet<PlayedCard> PlayedCards { get; set; }
        public DbSet<Surface> Surfaces { get; set; }
        public DbSet<WeatherCondition> WeatherConditions { get; set; }
        public DbSet<Tournament> Tournaments { get; set; }
        public DbSet<TournamentRegistration> TournamentRegistrations { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<ApplicationUser>()
     .HasOne(u => u.Player)
     .WithOne(p => p.User)
     .HasForeignKey<Player>(p => p.UserId)
     .IsRequired(false);

            modelBuilder.Entity<Player>()
       .HasOne(p => p.User)
       .WithOne(u => u.Player)
       .HasForeignKey<Player>(p => p.UserId);

            modelBuilder.Entity<TournamentRegistration>()
        .HasOne(tr => tr.Tournament)
        .WithMany()
        .HasForeignKey(tr => tr.TournamentId)
        .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<TournamentRegistration>()
                .HasOne(tr => tr.Player)
                .WithMany()
                .HasForeignKey(tr => tr.PlayerId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Player>()
                .HasOne(p => p.PlayingStyle)
                .WithMany(ps => ps.Players)
                .HasForeignKey(p => p.PlayingStyleId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Player>()
                .HasOne(p => p.SpecialAbility)
                .WithMany(sa => sa.Players)
                .HasForeignKey(p => p.SpecialAbilityId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Player>()
                .HasMany(p => p.MatchesAsPlayer1)
                .WithOne(m => m.Player1)
                .HasForeignKey(m => m.Player1Id)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Player>()
                .HasMany(p => p.MatchesAsPlayer2)
                .WithOne(m => m.Player2)
                .HasForeignKey(m => m.Player2Id)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Card>()
                .HasOne(c => c.CardCategory)
                .WithMany(cc => cc.Cards)
                .HasForeignKey(c => c.CardCategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Card>()
                .HasMany(c => c.SynergiesAsCard1)
                .WithOne(cs => cs.Card1)
                .HasForeignKey(cs => cs.Card1Id)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Card>()
                .HasMany(c => c.SynergiesAsCard2)
                .WithOne(cs => cs.Card2)
                .HasForeignKey(cs => cs.Card2Id)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<PlayerCard>()
                .HasOne(pc => pc.Player)
                .WithMany(p => p.PlayerCards)
                .HasForeignKey(pc => pc.PlayerId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<PlayerCard>()
                .HasOne(pc => pc.Card)
                .WithMany()
                .HasForeignKey(pc => pc.CardId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Match>()
                .HasOne(m => m.Surface)
                .WithMany(s => s.Matches)
                .HasForeignKey(m => m.SurfaceId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Match>()
                .HasOne(m => m.WeatherCondition)
                .WithMany(wc => wc.Matches)
                .HasForeignKey(m => m.WeatherConditionId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Match>()
                .HasOne(m => m.Tournament)
                .WithMany(t => t.Matches)
                .HasForeignKey(m => m.TournamentId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Set>()
                .HasOne(s => s.Match)
                .WithMany(m => m.Sets)
                .HasForeignKey(s => s.MatchId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Game>()
                .HasOne(g => g.Set)
                .WithMany(s => s.Games)
                .HasForeignKey(g => g.SetId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Point>()
                .HasOne(p => p.Game)
                .WithMany(g => g.Points)
                .HasForeignKey(p => p.GameId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<PlayedCard>()
                .HasOne(pc => pc.Point)
                .WithMany(p => p.PlayedCards)
                .HasForeignKey(pc => pc.PointId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<PlayedCard>()
                .HasOne(pc => pc.Player)
                .WithMany()
                .HasForeignKey(pc => pc.PlayerId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<PlayedCard>()
                .HasOne(pc => pc.Card)
                .WithMany()
                .HasForeignKey(pc => pc.CardId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Tournament>()
                .HasOne(t => t.Surface)
                .WithMany()
                .HasForeignKey(t => t.SurfaceId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}