using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SoftwareTest.Components;
using SoftwareTest.Components.Account;
using SoftwareTest.Data;
using System.Runtime.InteropServices;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddCascadingAuthenticationState();
builder.Services.AddScoped<IdentityUserAccessor>();
builder.Services.AddScoped<IdentityRedirectManager>();
builder.Services.AddScoped<AuthenticationStateProvider, IdentityRevalidatingAuthenticationStateProvider>();

builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = IdentityConstants.ApplicationScheme;
    options.DefaultSignInScheme = IdentityConstants.ExternalScheme;
})
    .AddIdentityCookies();

// Retrieve connection strings from your configuration
var defaultConnectionString = builder.Configuration.GetConnectionString("DefaultConnection")
                               ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

var mockDbConnectionString = builder.Configuration.GetConnectionString("MockDBConnection")
                               ?? throw new InvalidOperationException("Connection string 'MockDBConnection' not found.");

// Check the OS type and configure DbContext accordingly
if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
{
    // Use SQL Server for Windows (DefaultConnection)
    builder.Services.AddDbContext<ApplicationDbContext>(options =>
        options.UseSqlServer(defaultConnectionString));
}
else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
{
    // Use SQLite for Linux (MockDBConnection) for testing or mocking
    // Use in-memory SQLite for testing when running in Linux/WSL
    builder.Services.AddDbContext<ApplicationDbContext>(options =>
        options.UseSqlite(mockDbConnectionString));
}
else
{
    // Default to SQL Server (or another DB) for other platforms
    builder.Services.AddDbContext<ApplicationDbContext>(options =>
        options.UseSqlServer(defaultConnectionString));
}

builder.Services.AddDatabaseDeveloperPageExceptionFilter();

// Configure Identity with password requirements
builder.Services.AddIdentityCore<ApplicationUser>(options =>
{
    options.SignIn.RequireConfirmedAccount = true;
    options.Password.RequiredLength = 8; // Set the minimum password length to 8
})
.AddRoles<IdentityRole>()
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddSignInManager()
.AddDefaultTokenProviders();

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("RequireAdministratorRole", policy =>
    {
        policy.RequireRole("Admin");
    });
});

builder.Services.AddSingleton<IEmailSender<ApplicationUser>, IdentityNoOpEmailSender>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts(); // For production, you may want to adjust the HSTS value.
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

// Add additional endpoints required by the Identity /Account Razor components.
app.MapAdditionalIdentityEndpoints();

app.Run();
