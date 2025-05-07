using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SkillSwap.Data;
using SkillSwap.Models;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace SkillSwap.Pages.Account
{
    public class RegisterModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly PasswordHasher<object> _passwordHasher;
        private readonly ILogger<RegisterModel> _logger;

        public RegisterModel(
            ApplicationDbContext context, 
            PasswordHasher<object> passwordHasher,
            ILogger<RegisterModel> logger)
        {
            _context = context;
            _passwordHasher = passwordHasher;
            _logger = logger;
        }

        [BindProperty]
        public InputModel Input { get; set; }

        public string ReturnUrl { get; set; }

        [TempData]
        public string StatusMessage { get; set; }

        public class InputModel
        {
            [Required(ErrorMessage = "Full name is required")]
            [Display(Name = "Full Name")]
            [StringLength(100, ErrorMessage = "Full name cannot exceed 100 characters")]
            public string FullName { get; set; }

            [Required(ErrorMessage = "Email address is required")]
            [EmailAddress(ErrorMessage = "Invalid email address")]
            [Display(Name = "Email")]
            public string Email { get; set; }

            [Required(ErrorMessage = "Password is required")]
            [StringLength(100, ErrorMessage = "The {0} must be at least {2} characters long.", MinimumLength = 6)]
            [DataType(DataType.Password)]
            [Display(Name = "Password")]
            public string Password { get; set; }

            [DataType(DataType.Password)]
            [Display(Name = "Confirm password")]
            [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
            public string ConfirmPassword { get; set; }
        }

        public void OnGet(string returnUrl = null)
        {
            ReturnUrl = returnUrl ?? Url.Content("~/");
        }

        public async Task<IActionResult> OnPostAsync(string returnUrl = null)
        {
            returnUrl ??= Url.Content("~/");
            
            if (ModelState.IsValid)
            {
                try
                {
                    // Check if email already exists
                    var existingUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == Input.Email.ToLower().Trim());
                    if (existingUser != null)
                    {
                        ModelState.AddModelError(string.Empty, "This email is already registered. Please use a different email address.");
                        return Page();
                    }

                    // Create and hash the password
                    var hashedPassword = _passwordHasher.HashPassword(null, Input.Password);

                    // Create new user
                    var user = new ApplicationUser
                    {
                        FullName = Input.FullName.Trim(),
                        Email = Input.Email.ToLower().Trim(),
                        Password = hashedPassword
                    };

                    // Add to database
                    _context.Users.Add(user);
                    await _context.SaveChangesAsync();
                    
                    _logger.LogInformation("User created successfully with email: {Email}", Input.Email);
                    
                    // Redirect to login page with success message
                    TempData["SuccessMessage"] = "Your account has been created successfully! You can now log in.";
                    return RedirectToPage("./Login");
                }
                catch (Exception ex)
                {
                    // Log detailed error information
                    _logger.LogError(ex, "Error creating new user account");
                    ModelState.AddModelError(string.Empty, $"An error occurred while creating your account: {ex.Message}");
                }
            }

            // If we got this far, something failed; redisplay form
            return Page();
        }
    }
}