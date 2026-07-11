// Skanly.Web/Extensions/AuthorizationExtensions.cs
using Microsoft.AspNetCore.Authorization;

namespace Skanly.Web.Extensions;

public static class AuthorizationExtensions
{
    public static IServiceCollection AddSkanlyAuthorization(
        this IServiceCollection services)
    {
        services.AddAuthorization(options =>
        {
            // ── Role Policies ──────────────────────────────────────────────────
            options.AddPolicy("AdminOnly", policy =>
                policy.RequireRole("Admin"));

            options.AddPolicy("OwnerOnly", policy =>
                policy.RequireRole("Owner"));

            options.AddPolicy("StudentOnly", policy =>
                policy.RequireRole("Student"));

            options.AddPolicy("StudentOrOwner", policy =>
                policy.RequireRole("Student", "Owner"));

            options.AddPolicy("AuthenticatedUser", policy =>
                policy.RequireAuthenticatedUser());

            // ── Feature Policies ───────────────────────────────────────────────
            options.AddPolicy("VerifiedIdentity", policy =>
                policy.RequireRole("Student", "Owner")
                      .RequireClaim("IdentityVerified", "true"));

            options.AddPolicy("VerifiedStudent", policy =>
                policy.RequireRole("Student")
                      .RequireClaim("IdentityVerified", "true"));

            // ── Default Fallback ───────────────────────────────────────────────
            // All endpoints require authentication unless marked [AllowAnonymous]
            options.FallbackPolicy = new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .Build();
        });

        return services;
    }
}