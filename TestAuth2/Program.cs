
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using System.Text;
using TestAuth2.Data;
using TestAuth2.Entities;
using TestAuth2.Services;
using TestAuth2.Services.Imp;

namespace TestAuth2
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.

            builder.Services.AddControllers();
            // Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
            builder.Services.AddOpenApi();

            builder.Services.AddDbContext<AppDbContext>(options =>
                options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

            builder.Services.AddScoped<IPasswordHasher<User>,
                PasswordHasher<User>>();

            builder.Services.AddScoped<IAuthService, AuthService>();
            builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();

            #region Jwt Authentication Configuration
            var secretKey = builder.Configuration["Jwt:SecretKey"]
                ?? throw new InvalidOperationException(
                    "JWT SecretKey is missing.");

            var issuer = builder.Configuration["Jwt:Issuer"]
                ?? throw new InvalidOperationException(
                    "JWT Issuer is missing.");

            var audience = builder.Configuration["Jwt:Audience"]
                ?? throw new InvalidOperationException(
                    "JWT Audience is missing.");

            builder.Services
                .AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme =
                        JwtBearerDefaults.AuthenticationScheme;

                    options.DefaultChallengeScheme =
                        JwtBearerDefaults.AuthenticationScheme;
                })
                .AddJwtBearer(options =>
                {
                    options.TokenValidationParameters =
                        new TokenValidationParameters
                        {
                            ValidateIssuer = true,
                            ValidIssuer = issuer,

                            ValidateAudience = true,
                            ValidAudience = audience,

                            ValidateIssuerSigningKey = true,
                            IssuerSigningKey = new SymmetricSecurityKey(
                                Encoding.UTF8.GetBytes(secretKey)),

                            ValidateLifetime = true,
                            ClockSkew = TimeSpan.FromSeconds(30),

                            NameClaimType = ClaimTypes.Name,
                            RoleClaimType = ClaimTypes.Role
                        };
                });
            #endregion

            builder.Services.AddAuthorization();

            var app = builder.Build();

            #region Seed Roles
            // Seed roles if they do not exist
            await using (var scope = app.Services.CreateAsyncScope())
            {
                var dbContext = scope.ServiceProvider
                    .GetRequiredService<AppDbContext>();

                await dbContext.Database.MigrateAsync();

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
            }
            #endregion

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.MapOpenApi();
            }

            app.UseHttpsRedirection();

            app.UseAuthentication();
            app.UseAuthorization();

            app.MapControllers();

            app.Run();
        }
    }
}
