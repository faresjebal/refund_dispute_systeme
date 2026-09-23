using Microsoft.AspNetCore.Identity;
using System.Threading.Tasks;
using Internship.Domain.Entities; // ✅ Import your custom user
using System;

namespace Internship.Infrastructure.Data
{
    public static class SeedData
    {
        public static async Task Initialize(
            RefundDisputeContext context,
            UserManager<ApplicationUser> userManager,       // ✅ changed
            RoleManager<IdentityRole> roleManager)
        {
            await context.Database.EnsureCreatedAsync();

            if (!await roleManager.RoleExistsAsync("Admin"))
                await roleManager.CreateAsync(new IdentityRole("Admin"));

            if (!await roleManager.RoleExistsAsync("User"))
                await roleManager.CreateAsync(new IdentityRole("User"));

            var adminEmail = "admin@example.com";
            if (await userManager.FindByEmailAsync(adminEmail) == null)
            {
                var admin = new ApplicationUser                 // ✅ changed
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    EmailConfirmed = true,
                    RefreshToken = Guid.NewGuid().ToString(),  // ✅ optionally pre-fill
                    RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7)
                };

                await userManager.CreateAsync(admin, "Admin@123");
                await userManager.AddToRoleAsync(admin, "Admin");
            }
        }
    }
}
