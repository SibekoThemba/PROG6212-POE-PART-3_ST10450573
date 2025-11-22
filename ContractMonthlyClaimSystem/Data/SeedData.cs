using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ContractMonthlyClaimSystem.Models;

namespace ContractMonthlyClaimSystem.Data
{
    public static class SeedData
    {
        public static async Task Initialize(IServiceProvider serviceProvider)
        {
            using (var context = new ApplicationDbContext(
                serviceProvider.GetRequiredService<DbContextOptions<ApplicationDbContext>>()))
            {
                var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();

                string[] roleNames = { "Lecturer", "ProgrammeCoordinator", "AcademicManager", "HR" };

                foreach (var roleName in roleNames)
                {
                    var roleExist = await roleManager.RoleExistsAsync(roleName);
                    if (!roleExist)
                    {
                        await roleManager.CreateAsync(new IdentityRole(roleName));
                    }
                }
            }
        }
    }
}