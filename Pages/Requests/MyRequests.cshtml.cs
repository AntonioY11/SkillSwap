using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SkillSwap.Data;
using SkillSwap.Models;

namespace SkillSwap.Pages.Requests
{
    [Authorize]
    public class MyRequestsModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<MyRequestsModel> _logger;

        public MyRequestsModel(ApplicationDbContext context, ILogger<MyRequestsModel> logger)
        {
            _context = context;
            _logger = logger;
        }

        public List<Request> UserRequests { get; set; } = new List<Request>();

        public async Task<IActionResult> OnGetAsync()
        {
            try
            {
                var userId = int.Parse(User.FindFirst("UserId")?.Value ?? 
                    throw new InvalidOperationException("User ID not found"));

                UserRequests = await _context.Requests
                    .Where(r => r.User_id == userId)
                    .Include(r => r.Post)
                        .ThenInclude(p => p.User)
                    .OrderByDescending(r => r.Request_id)
                    .ToListAsync();

                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving user requests");
                TempData["ErrorMessage"] = "An error occurred while retrieving your requests.";
                return RedirectToPage("/Index");
            }
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