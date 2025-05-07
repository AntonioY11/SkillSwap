using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SkillSwap.Data;
using SkillSwap.Models;

namespace SkillSwap.Pages.Posts
{
    [Authorize]
    public class PostRequestsModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<PostRequestsModel> _logger;

        public PostRequestsModel(ApplicationDbContext context, ILogger<PostRequestsModel> logger)
        {
            _context = context;
            _logger = logger;
        }

        public Post Post { get; set; }
        public List<Request> Requests { get; set; } = new List<Request>();

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var userId = int.Parse(User.FindFirst("UserId")?.Value ?? 
                throw new InvalidOperationException("User ID not found"));

            Post = await _context.Posts
                .Include(p => p.User)
                .FirstOrDefaultAsync(p => p.Post_id == id);

            if (Post == null || Post.User_id != userId)
            {
                return NotFound();
            }

            Requests = await _context.Requests
                .Where(r => r.Post_id == id)
                .Include(r => r.User)
                .OrderByDescending(r => r.Request_id)
                .ToListAsync();

            return Page();
        }

        public async Task<IActionResult> OnPostUpdateStatusAsync(int requestId, bool? status)
        {
            var userId = int.Parse(User.FindFirst("UserId")?.Value ?? 
                throw new InvalidOperationException("User ID not found"));

            var request = await _context.Requests
                .Include(r => r.Post)
                .FirstOrDefaultAsync(r => r.Request_id == requestId);

            if (request == null || request.Post.User_id != userId)
            {
                return NotFound();
            }

            request.Status = status;
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Request status updated successfully.";
            return RedirectToPage(new { id = request.Post_id });
        }

        public string GetStatusBadgeClass(bool? status)
        {
            return status switch
            {
                true => "bg-success",
                false => "bg-danger",
                null => "bg-warning"
            };
        }

        public string GetStatusText(bool? status)
        {
            return status switch
            {
                true => "Accepted",
                false => "Rejected",
                null => "Pending"
            };
        }
    }
}