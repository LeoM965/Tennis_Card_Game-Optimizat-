using Microsoft.AspNetCore.Identity;

namespace Tennis_Card_Game.Models
{
    public class ApplicationUser : IdentityUser
    {
        public int? PlayerId { get; set; }
        public virtual Player Player { get; set; }
    }
}