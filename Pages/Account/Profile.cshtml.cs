using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SkillSwap.Data;
using SkillSwap.Models;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace SkillSwap.Pages.Account
{
    [Authorize]
    public class ProfileModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<ProfileModel> _logger;

        public ProfileModel(
            ApplicationDbContext context,
            IWebHostEnvironment environment,
            ILogger<ProfileModel> logger)
        {
            _context = context;
            _environment = environment;
            _logger = logger;
        }

        [BindProperty]
        public ProfileViewModel UserProfile { get; set; } = new ProfileViewModel();
        
        [BindProperty]
        public IFormFile? ProfileImage { get; set; }

        [TempData]
        public string StatusMessage { get; set; }

        public class ProfileViewModel
        {
            public int UserId { get; set; }
            public string FullName { get; set; }
            public string Email { get; set; }
            public string Bio { get; set; }
            public string ProfileImageUrl { get; set; }
            
            // Current password is required to change email or password
            public string CurrentPassword { get; set; }
            
            // Optional - only required if user wants to change password
            public string NewPassword { get; set; }
            public string ConfirmPassword { get; set; }
        }

        public async Task<IActionResult> OnGetAsync()
        {
            var userId = int.Parse(User.FindFirst("UserId")?.Value ?? 
                throw new InvalidOperationException("User ID not found"));

            var user = await _context.Users.FirstOrDefaultAsync(u => u.User_id == userId);
            if (user == null)
            {
                return NotFound();
            }

            UserProfile = new ProfileViewModel
            {
                UserId = user.User_id,
                FullName = user.FullName,
                Email = user.Email,
                Bio = user.Bio,
                ProfileImageUrl = user.ProfilePicture
            };

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            var userId = int.Parse(User.FindFirst("UserId")?.Value ?? 
                throw new InvalidOperationException("User ID not found"));

            var user = await _context.Users.FirstOrDefaultAsync(u => u.User_id == userId);
            if (user == null)
            {
                return NotFound();
            }

            // Update the basic info
            user.FullName = UserProfile.FullName;
            user.Bio = UserProfile.Bio;

            // Handle profile image upload if provided
            if (ProfileImage != null && ProfileImage.Length > 0)
            {
                // Validate file size (2MB max)
                if (ProfileImage.Length > 2 * 1024 * 1024)
                {
                    ModelState.AddModelError("ProfileImage", "Profile picture cannot exceed 2MB");
                    return Page();
                }

                // Validate file type
                var allowedTypes = new[] { "image/jpeg", "image/png", "image/gif" };
                if (!allowedTypes.Contains(ProfileImage.ContentType.ToLower()))
                {
                    ModelState.AddModelError("ProfileImage", "Only JPG, PNG and GIF files are allowed");
                    return Page();
                }

                // Delete the old image if it exists
                if (!string.IsNullOrEmpty(user.ProfilePicture))
                {
                    var oldImagePath = Path.Combine(_environment.WebRootPath, user.ProfilePicture.TrimStart('/'));
                    if (System.IO.File.Exists(oldImagePath))
                    {
                        System.IO.File.Delete(oldImagePath);
                    }
                }

                // Save the new profile picture
                var uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads", "profiles");
                Directory.CreateDirectory(uploadsFolder); // Ensure folder exists

                var uniqueFileName = $"profile_{userId}_{Guid.NewGuid().ToString().Substring(0, 8)}{Path.GetExtension(ProfileImage.FileName)}";
                var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await ProfileImage.CopyToAsync(fileStream);
                }

                // Update the profile picture URL
                user.ProfilePicture = $"/uploads/profiles/{uniqueFileName}";
            }

            try
            {
                await _context.SaveChangesAsync();
                
                // Update the name claim if it changed
                if (user.FullName != User.FindFirst(ClaimTypes.Name)?.Value)
                {
                    var identity = new ClaimsIdentity(User.Identity);
                    identity.RemoveClaim(User.FindFirst(ClaimTypes.Name));
                    identity.AddClaim(new Claim(ClaimTypes.Name, user.FullName));
                    
                    await HttpContext.SignInAsync(
                        CookieAuthenticationDefaults.AuthenticationScheme,
                        new ClaimsPrincipal(identity),
                        new AuthenticationProperties { IsPersistent = false });
                }
                
                StatusMessage = "Your profile has been updated successfully.";
                return RedirectToPage();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user profile");
                ModelState.AddModelError(string.Empty, "An error occurred while updating your profile.");
                return Page();
            }
        }
    }
}