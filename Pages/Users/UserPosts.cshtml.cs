using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SkillSwap.Data;
using SkillSwap.Models;

namespace SkillSwap.Pages.Users
{
    public class UserPostsModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public UserPostsModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public ApplicationUser UserProfile { get; set; }
        public List<Post> UserPosts { get; set; } = new List<Post>();
        public bool IsCurrentUser { get; private set; }

        public async Task<IActionResult> OnGetAsync(int id)
        {
            // Get the user profile
            UserProfile = await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.User_id == id);

            if (UserProfile == null)
            {
                return NotFound();
            }

            // Get all the user's public posts
            UserPosts = await _context.Posts
                .AsNoTracking()
                .Where(p => p.User_id == id)
                .OrderByDescending(p => p.Post_id)
                .ToListAsync();

            // Check if this is the current user's profile
            if (User.Identity.IsAuthenticated)
            {
                var currentUserId = int.Parse(User.FindFirst("UserId")?.Value);
                IsCurrentUser = (currentUserId == id);
            }

            return Page();
        }
    }
}