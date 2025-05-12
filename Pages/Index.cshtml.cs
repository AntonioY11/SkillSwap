using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SkillSwap.Data;
using SkillSwap.Models;

namespace SkillSwap.Pages
{
    public class IndexModel : PageModel
    {
        private readonly ILogger<IndexModel> _logger;
        private readonly ApplicationDbContext _context;

        public IndexModel(ILogger<IndexModel> logger, ApplicationDbContext context)
        {
            _logger = logger;
            _context = context;
        }

        public List<Post> RecentPosts { get; set; } = new List<Post>();

        public async Task OnGetAsync()
        {
            try
            {
                var query = _context.Posts
                    .AsNoTracking()  // For performance
                    .Include(p => p.User)
                    .OrderByDescending(p => p.Post_id);

                // Filter out the current user's posts if the user is authenticated
                if (User.Identity.IsAuthenticated)
                {
                    var userId = int.Parse(User.FindFirst("UserId")?.Value ?? 
                        throw new InvalidOperationException("User ID not found"));
                    query = query.Where(p => p.User_id != userId)
                        .OrderByDescending(p => p.Post_id);
                }

                // Get the 5 most recent posts
                RecentPosts = await query.Take(5).ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading recent posts");
                RecentPosts = new List<Post>();  // Empty list if DB access fails
            }
        }
    }
}
