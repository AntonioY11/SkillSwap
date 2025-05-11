using Microsoft.EntityFrameworkCore;
using SkillSwap.Data;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authentication;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();

// Get connection string
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

// Configure MySQL with correct version specification
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseMySql(connectionString, 
        ServerVersion.AutoDetect(connectionString),
        mySqlOptions => mySqlOptions.EnableRetryOnFailure()
    )
);

// Add authentication
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Strict;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
         options.Cookie.IsEssential = true;
    });

builder.Services.AddAuthorization();

// Add session
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// Register PasswordHasher service
builder.Services.AddScoped<PasswordHasher<object>>();

// Add this with your other service registrations
builder.Services.AddHttpContextAccessor();
// Removed IViewDataAccessor registration as it is not defined
// builder.Services.AddScoped<IViewDataAccessor, ViewDataAccessor>();

// Add these before adding the middleware
builder.Services.AddHttpContextAccessor();
// Removed IViewDataAccessor registration as it is not defined
// builder.Services.AddScoped<IViewDataAccessor, ViewDataAccessor>();

var app = builder.Build();

// Ensure uploads directory exists
var uploadsDir = Path.Combine(builder.Environment.WebRootPath, "uploads");
if (!Directory.Exists(uploadsDir))
{
    Directory.CreateDirectory(uploadsDir);
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}
else
{
    app.UseDeveloperExceptionPage();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

// Add middleware to populate HttpContext.Items with user info
app.Use(async (HttpContext context, Func<Task> next) =>
{
    if (context.User.Identity != null && context.User.Identity.IsAuthenticated)
    {
        try
        {
            // Get user ID from claims
            var userIdClaim = context.User.FindFirst("UserId");
            if (userIdClaim != null && int.TryParse(userIdClaim.Value, out int userId))
            {
                // Get user from database to check for profile picture
                using var scope = app.Services.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var user = await dbContext.Users.FirstOrDefaultAsync(u => u.User_id == userId);
                
                if (user != null)
                {
                    // Ensure default image path is correct and consistent
                    var defaultImagePath = "/images/default-avatar.png";
                    
                    // Set profile picture URL or default image
                    var profilePicUrl = !string.IsNullOrEmpty(user.ProfilePicture) 
                        ? user.ProfilePicture 
                        : defaultImagePath;
                        
                    // Make sure the profile picture exists, if not, use default
                    if (profilePicUrl != defaultImagePath)
                    {
                        var imagePath = Path.Combine(app.Environment.WebRootPath, profilePicUrl.TrimStart('/'));
                        if (!System.IO.File.Exists(imagePath))
                        {
                            profilePicUrl = defaultImagePath;
                        }
                    }
                    
                    // Add to HttpContext.Items for the layout to access
                    context.Items["UserProfilePic"] = profilePicUrl;
                }
            }
        }
        catch (Exception ex)
        {
            // Log but continue - don't want to break the entire site over a profile picture
            var logger = app.Services.GetRequiredService<ILogger<Program>>();
            logger.LogError(ex, "Error retrieving user profile picture");
            context.Items["UserProfilePic"] = "/images/default-avatar.png";
        }
    }
    
    // Continue with the request
    await next();
});
app.UseSession();

app.MapRazorPages();

app.Run();
