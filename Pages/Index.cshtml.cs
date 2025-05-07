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
                // Try to display some recent posts, but don't crash if DB isn't ready
                RecentPosts = await _context.Posts
                    .AsNoTracking()  // For performance
                    .Include(p => p.User)
                    .OrderByDescending(p => p.Post_id)
                    .Take(5)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading recent posts");
                RecentPosts = new List<Post>();  // Empty list if DB access fails
            }
        }
    }
}
