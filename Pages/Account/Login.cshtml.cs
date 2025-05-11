using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SkillSwap.Data;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Identity;

namespace SkillSwap.Pages.Account
{
    public class LoginModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly PasswordHasher<object> _passwordHasher;
        private readonly ILogger<LoginModel> _logger;

        public LoginModel(
            ApplicationDbContext context, 
            PasswordHasher<object> passwordHasher,
            ILogger<LoginModel> logger)
        {
            _context = context;
            _passwordHasher = passwordHasher;
            _logger = logger;
        }

        [BindProperty]
        public InputModel Input { get; set; }

        [TempData]
        public string StatusMessage { get; set; }

        public string ReturnUrl { get; set; }

        public class InputModel
        {
            [Required(ErrorMessage = "Email is required")]
            [EmailAddress(ErrorMessage = "Invalid email address")]
            public string Email { get; set; }

            [Required(ErrorMessage = "Password is required")]
            [DataType(DataType.Password)]
            public string Password { get; set; }
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
                    var user = await _context.Users
                        .FirstOrDefaultAsync(u => u.Email == Input.Email.ToLower().Trim());

                    if (user != null)
                    {
                        var result = _passwordHasher.VerifyHashedPassword(null, user.Password, Input.Password);
                        
                        if (result == PasswordVerificationResult.Success)
                        {
                            _logger.LogInformation("User logged in: {Email}", user.Email);
                            
                            var claims = new List<Claim>
                            {
                                new Claim(ClaimTypes.Name, user.FullName),
                                new Claim(ClaimTypes.Email, user.Email),
                                new Claim("UserId", user.User_id.ToString())
                            };

                            var claimsIdentity = new ClaimsIdentity(
                                claims, CookieAuthenticationDefaults.AuthenticationScheme);

                            var authProperties = new AuthenticationProperties
                            {
                                IsPersistent = false,
                                //ExpiresUtc = DateTimeOffset.UtcNow.AddDays(7)
                            };

                            await HttpContext.SignInAsync(
                                CookieAuthenticationDefaults.AuthenticationScheme,
                                new ClaimsPrincipal(claimsIdentity),
                                authProperties);

                            return LocalRedirect(returnUrl);
                        }
                    }

                    // Don't reveal that the user doesn't exist or the password is incorrect
                    ModelState.AddModelError(string.Empty, "Invalid login attempt. Please check your email and password.");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error during login attempt");
                    ModelState.AddModelError(string.Empty, "An error occurred during login. Please try again.");
                }
            }

            // If we got this far, something failed; redisplay form
            return Page();
        }
    }
}