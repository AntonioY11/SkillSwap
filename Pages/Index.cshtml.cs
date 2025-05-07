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
                // Order by Post_id instead of CreatedAt since we don't have that field
                RecentPosts = await _context.Posts
                    .Include(p => p.User)
                    .OrderByDescending(p => p.Post_id)  // Use Post_id to get newest posts
                    .Take(5)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading recent posts");
                // If there's an error, we'll have an empty list (already initialized)
            }
        }
    }
}
