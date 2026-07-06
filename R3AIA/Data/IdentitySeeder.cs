using Microsoft.AspNetCore.Identity;
using R3AIA.Models;
using static R3AIA.Models.Enums;

namespace R3AIA.Data;

public static class IdentitySeeder
{
	public static async Task SeedAsync(this IApplicationBuilder app)
	{
		using var scope = app.ApplicationServices.CreateScope();
		var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
		var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

		// Seed roles
		var roles = new[] { "Admin", "Doctor", "Pharmacist", "Volunteer", "Patient" };
		foreach (var role in roles)
		{
			if (!await roleManager.RoleExistsAsync(role))
			{
				await roleManager.CreateAsync(new IdentityRole(role));
			}
		}

		// Seed default admin user
		const string adminEmail = "admin@r3aia.local";
		var admin = await userManager.FindByEmailAsync(adminEmail);
		if (admin is null)
		{
			admin = new ApplicationUser
			{
				UserName = adminEmail,
				Email = adminEmail,
				FullName = "R3AIA Admin",
				EmailConfirmed = true,
				UserType = UserType.Admin,
				AccountStatus = AccountStatus.Active,
				IsVerified = true
			};

			var result = await userManager.CreateAsync(admin, "Admin@12345");
			if (result.Succeeded)
			{
				await userManager.AddToRoleAsync(admin, "Admin");
			}
		}
	}
}


