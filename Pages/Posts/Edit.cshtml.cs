using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SkillSwap.Data;
using SkillSwap.Models;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace SkillSwap.Pages.Posts
{
    [Authorize]
    public class EditModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<EditModel> _logger;

        public EditModel(
            ApplicationDbContext context,
            IWebHostEnvironment environment,
            ILogger<EditModel> logger)
        {
            _context = context;
            _environment = environment;
            _logger = logger;
        }

        [BindProperty]
        public Post Post { get; set; } = new Post();

        [BindProperty]
        public IFormFile? ImageFile { get; set; }

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var userId = int.Parse(User.FindFirst("UserId")?.Value ?? 
                throw new InvalidOperationException("User ID not found"));

            var post = await _context.Posts
                .FirstOrDefaultAsync(p => p.Post_id == id);

            if (post == null)
            {
                return NotFound();
            }

            // Verify that the user owns this post
            if (post.User_id != userId)
            {
                return Forbid();
            }

            Post = post;
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            try
            {
                var userId = int.Parse(User.FindFirst("UserId")?.Value ?? 
                    throw new InvalidOperationException("User ID not found"));

                // Verify that the user owns this post
                var originalPost = await _context.Posts
                    .AsNoTracking()
                    .FirstOrDefaultAsync(p => p.Post_id == Post.Post_id);

                if (originalPost == null)
                {
                    return NotFound();
                }

                if (originalPost.User_id != userId)
                {
                    return Forbid();
                }

                // Handle image upload if provided
                if (ImageFile != null && ImageFile.Length > 0)
                {
                    // Validate file size (5MB max)
                    if (ImageFile.Length > 5 * 1024 * 1024)
                    {
                        ModelState.AddModelError("ImageFile", "File size cannot exceed 5MB");
                        return Page();
                    }

                    // Validate file type
                    var allowedTypes = new[] { "image/jpeg", "image/png", "image/gif" };
                    if (!allowedTypes.Contains(ImageFile.ContentType.ToLower()))
                    {
                        ModelState.AddModelError("ImageFile", "Only JPG, PNG and GIF files are allowed");
                        return Page();
                    }

                    // Delete the old image if it exists
                    if (!string.IsNullOrEmpty(originalPost.Image))
                    {
                        var oldImagePath = Path.Combine(_environment.WebRootPath, originalPost.Image.TrimStart('/'));
                        if (System.IO.File.Exists(oldImagePath))
                        {
                            System.IO.File.Delete(oldImagePath);
                        }
                    }

                    // Save the new image
                    var uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads");
                    Directory.CreateDirectory(uploadsFolder); // Ensure folder exists

                    var uniqueFileName = $"{Guid.NewGuid()}_{Path.GetFileName(ImageFile.FileName)}";
                    var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await ImageFile.CopyToAsync(fileStream);
                    }

                    Post.Image = $"/uploads/{uniqueFileName}";
                }
                else
                {
                    // Keep the existing image if no new image uploaded
                    Post.Image = originalPost.Image;
                }

                // Update post in database
                _context.Attach(Post).State = EntityState.Modified;
                await _context.SaveChangesAsync();

                _logger.LogInformation("Post updated successfully with ID: {PostId}", Post.Post_id);

                TempData["SuccessMessage"] = "Your post has been updated successfully!";
                return RedirectToPage("./MyPosts");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating post");
                ModelState.AddModelError(string.Empty, 
                    "An error occurred while updating your post. Please try again.");
                return Page();
            }
        }
    }
}