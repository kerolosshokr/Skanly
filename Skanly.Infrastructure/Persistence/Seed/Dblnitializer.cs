// Skanly.Infrastructure/Persistence/Seed/DbInitializer.cs
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Skanly.Domain.Entities;
using Skanly.Infrastructure.Identity;

namespace Skanly.Infrastructure.Persistence.Seed;

public static class DbInitializer
{
    public static async Task SeedAsync(IServiceProvider services)
    {
        var context = services.GetRequiredService<SkanlyDbContext>();
        await context.Database.MigrateAsync();

        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();

        string[] roles = { "Admin", "Owner", "Student" };
        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
                await roleManager.CreateAsync(new IdentityRole(role));
        }

        const string adminEmail = "admin@skanly.com";
        if (await userManager.FindByEmailAsync(adminEmail) is null)
        {
            var adminUser = new ApplicationUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                EmailConfirmed = true,
                IsActive = true
            };

            var result = await userManager.CreateAsync(adminUser, "Admin@123456");
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(adminUser, "Admin");

                context.Admins.Add(new Admin
                {
                    UserId = adminUser.Id,
                    FirstName = "Skanly",
                    LastName = "Admin",
                    Department = "Platform Management"
                });

                context.CommissionSettings.Add(new CommissionSetting
                {
                    Rate = 10.00m,
                    SetByAdminId = adminUser.Id,
                    EffectiveFrom = DateTime.UtcNow,
                    IsActive = true
                });

                await context.SaveChangesAsync();
            }
        }
    }
}