using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SkillSwap.Data;
using SkillSwap.Models;

namespace SkillSwap.Pages.Posts
{
    [Authorize]
    public class MyPostsModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<MyPostsModel> _logger;

        public MyPostsModel(ApplicationDbContext context, ILogger<MyPostsModel> logger)
        {
            _context = context;
            _logger = logger;
        }

        public List<Post> UserPosts { get; set; } = new List<Post>();
        public int TotalPosts { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            try
            {
                var userId = int.Parse(User.FindFirst("UserId")?.Value ?? 
                    throw new InvalidOperationException("User ID not found"));

                UserPosts = await _context.Posts
                    .Where(p => p.User_id == userId)
                    .Include(p => p.User)
                    .Include(p => p.Requests)
                    .OrderByDescending(p => p.Post_id)
                    .ToListAsync();

                TotalPosts = UserPosts.Count;

                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving user posts");
                TempData["ErrorMessage"] = "An error occurred while retrieving your posts.";
                return RedirectToPage("/Index");
            }
        }

        public async Task<IActionResult> OnPostDeleteAsync(int id)
        {
            try
            {
                var userId = int.Parse(User.FindFirst("UserId")?.Value ?? 
                    throw new InvalidOperationException("User ID not found"));

                var post = await _context.Posts
                    .FirstOrDefaultAsync(p => p.Post_id == id && p.User_id == userId);

                if (post == null)
                {
                    return NotFound();
                }

                // Delete the image file if it exists
                if (!string.IsNullOrEmpty(post.Image))
                {
                    var imagePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", post.Image.TrimStart('/'));
                    if (System.IO.File.Exists(imagePath))
                    {
                        System.IO.File.Delete(imagePath);
                    }
                }

                _context.Posts.Remove(post);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Post deleted successfully.";
                return RedirectToPage();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting post");
                TempData["ErrorMessage"] = "An error occurred while deleting the post.";
                return RedirectToPage();
            }
        }
    }
}