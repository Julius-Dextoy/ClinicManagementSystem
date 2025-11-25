using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ClinicManagementSystem.Models;
using Microsoft.AspNetCore.HttpOverrides;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

// Add configuration validation
if (string.IsNullOrEmpty(builder.Configuration.GetConnectionString("DefaultConnection")))
{
    throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
}

// Add services to the container.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

// Configure Entity Framework with MySQL and enable retry logic
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString),
        mysqlOptions =>
        {
            mysqlOptions.EnableRetryOnFailure(
                maxRetryCount: 5,
                maxRetryDelay: TimeSpan.FromSeconds(30),
                errorNumbersToAdd: null);
        }));

// Enhanced Identity configuration
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    // Password settings
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireNonAlphanumeric = true;
    options.Password.RequireUppercase = true;
    options.Password.RequiredLength = 8;
    options.Password.RequiredUniqueChars = 1;

    // Lockout settings
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.AllowedForNewUsers = true;

    // User settings
    options.User.AllowedUserNameCharacters = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._@+";
    options.User.RequireUniqueEmail = true;

    // Sign-in settings
    options.SignIn.RequireConfirmedAccount = false;
    options.SignIn.RequireConfirmedEmail = false;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders()
.AddErrorDescriber<LocalizedIdentityErrorDescriber>();

// Configure cookies
builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.HttpOnly = true;
    options.ExpireTimeSpan = TimeSpan.FromDays(30);
    options.LoginPath = "/Account/Login";
    options.AccessDeniedPath = "/Account/AccessDenied";
    options.SlidingExpiration = true;

    // In production, set to Strict
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
});

// Add security headers service
builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = "X-CSRF-TOKEN";
    options.SuppressXFrameOptionsHeader = false;
});

// SIMPLIFIED Health Checks - Remove complex health checks for now
builder.Services.AddHealthChecks();

// Add memory cache for performance
builder.Services.AddMemoryCache();

// Add response compression
builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
});

// Configure MVC with JSON options
builder.Services.AddControllersWithViews()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
        options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
    });

builder.Services.AddRazorPages();

// Add custom services
builder.Services.AddScoped<DatabaseSeeder>();

var app = builder.Build();

// Security headers middleware
app.Use(async (context, next) =>
{
    context.Response.Headers.Add("X-Content-Type-Options", "nosniff");
    context.Response.Headers.Add("X-Frame-Options", "DENY");
    context.Response.Headers.Add("X-XSS-Protection", "1; mode=block");
    context.Response.Headers.Add("Referrer-Policy", "strict-origin-when-cross-origin");

    // Remove server header
    context.Response.Headers.Remove("Server");

    await next();
});

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();

    // Detailed errors in development
    app.UseStatusCodePagesWithReExecute("/Home/Error", "?statusCode={0}");
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts(); // HTTP Strict Transport Security

    // Custom error handling for production
    app.UseStatusCodePagesWithReExecute("/Home/Error", "?statusCode={0}");
}

// Security enhancements
app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
});

app.UseHttpsRedirection();
app.UseResponseCompression();
app.UseStaticFiles();

app.UseRouting();

// Enhanced security middleware
app.UseAuthentication();
app.UseAuthorization();

// Basic health check endpoint
app.MapHealthChecks("/health");

// Secure controller routes
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapRazorPages();

// LABEL: Database Migration and Seeding
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var logger = services.GetRequiredService<ILogger<Program>>();

    try
    {
        var context = services.GetRequiredService<ApplicationDbContext>();
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();

        logger.LogInformation("Applying database migrations...");

        // Apply migrations with retry logic
        var retryCount = 0;
        var maxRetries = 3;

        while (retryCount < maxRetries)
        {
            try
            {
                await context.Database.MigrateAsync();
                break;
            }
            catch (Exception ex) when (retryCount < maxRetries - 1)
            {
                retryCount++;
                logger.LogWarning(ex, "Migration attempt {RetryCount} failed. Retrying in 5 seconds...", retryCount);
                await Task.Delay(5000);
            }
        }

        logger.LogInformation("Seeding database...");

        // Seed roles and users
        await DatabaseSeeder.SeedRolesAsync(roleManager);
        await DatabaseSeeder.SeedAdminUserAsync(userManager);

        logger.LogInformation("Database seeded successfully");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "An error occurred while seeding the database");
        // Don't crash the app if seeding fails
    }
}

app.Run();