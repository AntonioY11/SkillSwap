using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SkillSwap.Data;
using SkillSwap.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SkillSwap.Pages.Posts
{
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public IndexModel(ApplicationDbContext context)
        {
            _context = context;
        }

        [BindProperty(SupportsGet = true)]
        public string SearchTerm { get; set; }

        [BindProperty(SupportsGet = true)]
        public int PageNumber { get; set; } = 1;

        [BindProperty(SupportsGet = true)]
        public string SortBy { get; set; } = "newest";

        public int TotalPages { get; set; }
        public int PostsPerPage { get; set; } = 6; // Number of posts per page
        public List<Post> Posts { get; set; } = new List<Post>();
        public int TotalPostCount { get; set; }

        public async Task OnGetAsync()
        {
            var query = _context.Posts.AsQueryable();

            // Filter out the current user's posts if the user is authenticated
            if (User.Identity.IsAuthenticated)
            {
                var userId = int.Parse(User.FindFirst("UserId")?.Value ?? 
                    throw new InvalidOperationException("User ID not found"));
                query = query.Where(p => p.User_id != userId);
            }

            // Apply search filter if provided
            if (!string.IsNullOrEmpty(SearchTerm))
            {
                query = query.Where(p => p.Title.Contains(SearchTerm) || 
                                        (p.Description != null && p.Description.Contains(SearchTerm)));
            }

            // Get total count for pagination
            TotalPostCount = await query.CountAsync();
            TotalPages = (int)Math.Ceiling(TotalPostCount / (double)PostsPerPage);

            // Ensure page number is valid
            if (PageNumber < 1) PageNumber = 1;
            if (PageNumber > TotalPages && TotalPages > 0) PageNumber = TotalPages;

            // Apply sorting
            query = SortBy switch
            {
                "title" => query.OrderBy(p => p.Title),
                "oldest" => query.OrderBy(p => p.Post_id),
                _ => query.OrderByDescending(p => p.Post_id) // "newest" is default
            };

            // Apply pagination
            Posts = await query
                .Skip((PageNumber - 1) * PostsPerPage)
                .Take(PostsPerPage)
                .Include(p => p.User)
                .ToListAsync();
        }
    }
}