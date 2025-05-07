using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SkillSwap.Data;
using SkillSwap.Models;

namespace SkillSwap.Pages.Posts
{
    public class DetailsModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<DetailsModel> _logger;

        public DetailsModel(ApplicationDbContext context, ILogger<DetailsModel> logger)
        {
            _context = context;
            _logger = logger;
        }

        public Post Post { get; set; }
        
        [BindProperty]
        public string RequestComment { get; set; }

        public bool HasRequested { get; set; }

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            Post = await _context.Posts
                .Include(p => p.User)
                .FirstOrDefaultAsync(m => m.Post_id == id);

            if (Post == null)
            {
                return NotFound();
            }

            // Check if user has already requested this post
            if (User.Identity.IsAuthenticated)
            {
                var userId = int.Parse(User.FindFirst("UserId")?.Value);
                HasRequested = await _context.Requests
                    .AnyAsync(r => r.Post_id == id && r.User_id == userId);
            }
            
            return Page();
        }

        public async Task<IActionResult> OnPostRequestAsync(int postId)
        {
            if (!User.Identity.IsAuthenticated)
            {
                return RedirectToPage("/Account/Login");
            }

            var userId = int.Parse(User.FindFirst("UserId")?.Value);

            // Check if user already requested this post
            var existingRequest = await _context.Requests
                .FirstOrDefaultAsync(r => r.Post_id == postId && r.User_id == userId);

            if (existingRequest != null)
            {
                TempData["ErrorMessage"] = "You have already requested this post.";
                return RedirectToPage(new { id = postId });
            }

            // Create new request
            var request = new Request
            {
                Post_id = postId,
                User_id = userId,
                Comment = RequestComment,
                Status = null // Pending
            };

            try
            {
                _context.Requests.Add(request);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Your request has been sent successfully!";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating request");
                TempData["ErrorMessage"] = "An error occurred while sending your request.";
            }

            return RedirectToPage(new { id = postId });
        }
    }
}