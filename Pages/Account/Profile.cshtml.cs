using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SkillSwap.Data;
using SkillSwap.Models;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using MySqlConnector;
using System.ComponentModel.DataAnnotations;

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
            
            [Required(ErrorMessage = "Name is required")]
            [StringLength(255, ErrorMessage = "Name cannot exceed 255 characters")]
            public string FullName { get; set; }
            
            public string Email { get; set; }
            
            // Make Bio explicitly nullable
            public string? Bio { get; set; }
            
            public string? ProfileImageUrl { get; set; }
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

            try
            {
                var userId = int.Parse(User.FindFirst("UserId")?.Value ?? 
                    throw new InvalidOperationException("User ID not found"));
                    
                string profilePicturePath = null;
                
                // Handle profile image if uploaded
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

                    // Generate a unique file name
                    var uniqueFileName = Guid.NewGuid().ToString() + Path.GetExtension(ProfileImage.FileName);
                    
                    // Create directories if they don't exist
                    var uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads");
                    var profilesFolder = Path.Combine(uploadsFolder, "profiles");
                    
                    Directory.CreateDirectory(uploadsFolder);
                    Directory.CreateDirectory(profilesFolder);
                    
                    // Full path where the file will be saved
                    var filePath = Path.Combine(profilesFolder, uniqueFileName);
                    
                    // Save the file
                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await ProfileImage.CopyToAsync(fileStream);
                    }
                    
                    // Set the path to be stored in the database
                    profilePicturePath = $"/uploads/profiles/{uniqueFileName}";
                    
                    _logger.LogInformation("Image saved to: {Path}", filePath);
                }
                
                // Direct database update approach with proper escaping
                using (var connection = new MySqlConnection(_context.Database.GetConnectionString()))
                {
                    await connection.OpenAsync();
                    
                    using (var command = connection.CreateCommand())
                    {
                        // Use backticks to escape the User table name
                        command.CommandText = "UPDATE `User` SET FullName = @name, Bio = @bio";
                        
                        if (ProfileImage != null && ProfileImage.Length > 0)
                        {
                            command.CommandText += ", ProfilePicture = @pic";
                            command.Parameters.AddWithValue("@pic", profilePicturePath);
                        }
                        
                        command.CommandText += " WHERE User_id = @userId";
                        command.Parameters.AddWithValue("@name", UserProfile.FullName);
                        command.Parameters.AddWithValue("@bio", UserProfile.Bio ?? (object)DBNull.Value);
                        command.Parameters.AddWithValue("@userId", userId);
                        
                        int rowsAffected = await command.ExecuteNonQueryAsync();
                        _logger.LogInformation("Database update result: {Count} rows affected", rowsAffected);
                        
                        if (rowsAffected > 0)
                        {
                            // Update the authentication cookie if name changed
                            if (UserProfile.FullName != User.FindFirst(ClaimTypes.Name)?.Value)
                            {
                                // Create a completely new ClaimsIdentity with all the claims we want
                                var claims = new List<Claim>();
    
                                // Add the updated name claim
                                claims.Add(new Claim(ClaimTypes.Name, UserProfile.FullName));
    
                                // Copy all other claims except the name claim
                                foreach (var claim in User.Claims)
                                {
                                    if (claim.Type != ClaimTypes.Name)
                                    {
                                        claims.Add(claim);
                                    }
                                }
    
                                // Create a new identity and principal
                                var newIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                                var newPrincipal = new ClaimsPrincipal(newIdentity);
    
                                // Sign in with the new principal
                                await HttpContext.SignInAsync(
                                    CookieAuthenticationDefaults.AuthenticationScheme,
                                    newPrincipal,
                                    new AuthenticationProperties { IsPersistent = false });
                            }
                            
                            TempData["StatusMessage"] = "Your profile has been updated successfully.";
                            return RedirectToPage();
                        }
                        else
                        {
                            ModelState.AddModelError(string.Empty, "Update failed. Please try again.");
                            return Page();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user profile: {Message}", ex.Message);
                ModelState.AddModelError(string.Empty, $"Error: {ex.Message}");
                await OnGetAsync();
                return Page();
            }
        }
    }
}