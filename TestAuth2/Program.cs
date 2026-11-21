
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using System.Text;
using TestAuth2.Authorization;
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

            builder.Services.AddSingleton<
                IAuthorizationHandler,
                PermissionAuthorizationHandler>();
            builder.Services.AddSingleton<
                IAuthorizationPolicyProvider,
                PermissionPolicyProvider>();
            builder.Services.AddAuthorization(//options =>
            //{
            //    options.AddPolicy(
            //        "user.role1",
            //        policy =>
            //        {
            //            policy.Requirements.Add(
            //                new PermissionRequirement("user.role1"));
            //        });

            //    options.AddPolicy(
            //        "user.role2",
            //        policy =>
            //        {
            //            policy.Requirements.Add(
            //                new PermissionRequirement("user.role2"));
            //        });
            //}
            );

            var app = builder.Build();

            #region Data Seeder
            using (var scope = app.Services.CreateScope())
            {
                var db = scope.ServiceProvider
                    .GetRequiredService<AppDbContext>();

                await DbSeeder.SeedAsync(db);
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
