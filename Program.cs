using KLENZ.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using System.IO;

var builder = WebApplication.CreateBuilder(args);

// ✅ Configure Database Context (Fixes duplicate registration)
builder.Services.AddDbContext<KLENZDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("KLENZDbContext"),
    sqlServerOptionsAction: sqlOptions => {
        sqlOptions.EnableRetryOnFailure();
    })
);

// ✅ Add Services
builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages(); // 🔥 Enables UI Hot-Reload

// ✅ Identity Configuration
builder.Services.AddDefaultIdentity<ApplicationUser>(options =>
{
    options.SignIn.RequireConfirmedAccount = false;
    options.Password.RequireDigit = true;
    options.Password.RequireUppercase = true;
    options.Password.RequiredLength = 8;
})
.AddEntityFrameworkStores<KLENZDbContext>()
.AddDefaultUI()
.AddDefaultTokenProviders();

// ✅ Configure Authentication & Authorization
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Identity/Account/Login";
    options.AccessDeniedPath = "/Identity/Account/AccessDenied";
    options.ReturnUrlParameter = "returnUrl";
    options.ExpireTimeSpan = TimeSpan.FromMinutes(30);
    options.SlidingExpiration = true;
});

// Every page requires a signed-in user unless it is marked [AllowAnonymous]
builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
});

// ✅ Configure Data Protection (Fixes hardcoded path)
builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "KLENZKeys")))
    .SetApplicationName("KLENZ");

// ✅ CORS Configuration
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// ✅ Configure Middleware
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseForwardedHeaders();
app.UseAuthentication();

// Uploaded attachments are only served to signed-in users
app.Use(async (context, next) =>
{
    if (context.Request.Path.StartsWithSegments("/uploads")
        && context.User.Identity?.IsAuthenticated != true)
    {
        await context.ChallengeAsync();
        return;
    }
    await next();
});

app.UseStaticFiles();
app.UseRouting();
app.UseCors("AllowAll");
app.UseAuthorization();

// ✅ Map Razor Pages (Identity UI)
app.MapRazorPages();

// ✅ Map Controllers & Routes
app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// ✅ Run Application
app.Run();
