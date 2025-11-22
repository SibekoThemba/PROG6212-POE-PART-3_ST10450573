using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using ContractMonthlyClaimSystem.Data;
using ContractMonthlyClaimSystem.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

// CORRECT Identity configuration
builder.Services.AddDefaultIdentity<ApplicationUser>(options =>
{
    options.SignIn.RequireConfirmedAccount = false;
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = true;
    options.Password.RequiredLength = 6;
})
.AddRoles<IdentityRole>() // Add this line to support roles
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultUI()
.AddDefaultTokenProviders();

builder.Services.AddControllersWithViews();

var app = builder.Build();

// SEED ROLES - Add this section after builder.Build() but before app.Run()
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<ApplicationDbContext>();
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();

        // Ensure database is created
        context.Database.EnsureCreated();

        // Seed roles
        await SeedRoles(roleManager);

        // Optional: Create a default admin user
        await SeedDefaultUser(userManager);
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while seeding the database.");
    }
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
app.MapRazorPages();

app.Run();

// Role seeding method
static async Task SeedRoles(RoleManager<IdentityRole> roleManager)
{
    string[] roleNames = { "Lecturer", "ProgrammeCoordinator", "AcademicManager", "HR" };

    foreach (var roleName in roleNames)
    {
        var roleExist = await roleManager.RoleExistsAsync(roleName);
        if (!roleExist)
        {
            // Create the role if it doesn't exist
            await roleManager.CreateAsync(new IdentityRole(roleName));
        }
    }
}

// Optional: Default user seeding method
static async Task SeedDefaultUser(UserManager<ApplicationUser> userManager)
{
    // Check if any users exist
    if (!userManager.Users.Any())
    {
        var defaultUser = new ApplicationUser
        {
            UserName = "admin@university.com",
            Email = "admin@university.com",
            FullName = "System Administrator",
            UserType = UserType.AcademicManager,
            EmailConfirmed = true
        };

        var result = await userManager.CreateAsync(defaultUser, "Admin123!");

        if (result.Succeeded)
        {
            // Assign the AcademicManager role to the default admin
            await userManager.AddToRoleAsync(defaultUser, "AcademicManager");
        }
    }
}