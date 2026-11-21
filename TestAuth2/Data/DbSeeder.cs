using Microsoft.EntityFrameworkCore;
using TestAuth2.Entities;

namespace TestAuth2.Data
{
    public static class DbSeeder
    {
        public static async Task SeedAsync(AppDbContext dbContext)
        {
            await dbContext.Database.MigrateAsync();

            // Seed roles
            var requiredRoles = new[] { "Admin", "User" };

            foreach (var roleName in requiredRoles)
            {
                var exists = await dbContext.Roles
                    .AnyAsync(r => r.Name == roleName);

                if (!exists)
                {
                    dbContext.Roles.Add(new Role
                    {
                        Name = roleName
                    });
                }
            }

            await dbContext.SaveChangesAsync();

            // Seed permissions
            var requiredPermissions = new[] { "user.role1", "user.role2" };

            foreach (var permissionName in requiredPermissions)
            {
                var exists = await dbContext.Permissions
                    .AnyAsync(p => p.Name == permissionName);

                if (!exists)
                {
                    dbContext.Permissions.Add(new Permission
                    {
                        Name = permissionName
                    });
                }
            }

            await dbContext.SaveChangesAsync();
        }
    }
}
