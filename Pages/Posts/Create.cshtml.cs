using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SkillSwap.Data;
using SkillSwap.Models;
using System.Security.Claims;

namespace SkillSwap.Pages.Posts
{
    [Authorize]
    public class CreateModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<CreateModel> _logger;

        public CreateModel(
            ApplicationDbContext context,
            IWebHostEnvironment environment,
            ILogger<CreateModel> logger)
        {
            _context = context;
            _environment = environment;
            _logger = logger;
        }

        [BindProperty]
        public Post Post { get; set; } = new Post();

        [BindProperty]
        public IFormFile? ImageFile { get; set; }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            try
            {
                // Get current user ID from claims
                var userId = int.Parse(User.FindFirst("UserId")?.Value ?? 
                    throw new InvalidOperationException("User ID not found"));

                Post.User_id = userId;

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

                    // Save the image
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

                // Add post to database
                _context.Posts.Add(Post);
                await _context.SaveChangesAsync();

                _logger.LogInformation("New post created with ID: {PostId}", Post.Post_id);

                TempData["SuccessMessage"] = "Your post has been created successfully!";
                return RedirectToPage("./Index");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating new post");
                ModelState.AddModelError(string.Empty, 
                    "An error occurred while creating your post. Please try again.");
                return Page();
            }
        }
    }
}