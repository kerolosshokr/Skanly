// Skanly.Infrastructure/DependencyInjection.cs  (complete updated file)
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Skanly.Application.Common.Interfaces;
using Skanly.Application.Common.Interfaces.Repositories;
using Skanly.Application.Features.Auth.Interfaces;
using Skanly.Application.Features.Payments.Interfaces;
using Skanly.Infrastructure.ExternalServices.Email;
using Skanly.Infrastructure.ExternalServices.Payment;
using Skanly.Infrastructure.FileStorage;
using Skanly.Infrastructure.Identity;
using Skanly.Infrastructure.Persistence;
using Skanly.Infrastructure.Persistence.Repositories;

namespace Skanly.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // ── DbContext ──────────────────────────────────────────────────────────
        // Scoped lifetime is critical — one DbContext per HTTP request ensures
        // ALL repositories and the UnitOfWork share the same EF Core change
        // tracker within a single request. Never register as Singleton or
        // Transient for a web application.
        services.AddDbContext<SkanlyDbContext>(options =>
            options.UseSqlServer(
                configuration.GetConnectionString("DefaultConnection"),
                sqlOptions => sqlOptions
                    .EnableRetryOnFailure(
                        maxRetryCount: 3,
                        maxRetryDelay: TimeSpan.FromSeconds(10),
                        errorNumbersToAdd: null)
                    .CommandTimeout(30)
                    .MigrationsAssembly(typeof(SkanlyDbContext).Assembly.FullName)));

        // ── Identity ───────────────────────────────────────────────────────────
        services.AddIdentity<ApplicationUser, IdentityRole>(options =>
        {
            options.Password.RequiredLength = 8;
            options.Password.RequireUppercase = true;
            options.Password.RequireDigit = true;
            options.Password.RequireNonAlphanumeric = false;
            options.User.RequireUniqueEmail = true;
            options.SignIn.RequireConfirmedEmail = true;
            options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
            options.Lockout.MaxFailedAccessAttempts = 5;
        })
            .AddEntityFrameworkStores<SkanlyDbContext>()
            .AddDefaultTokenProviders();

        // ── Unit of Work ───────────────────────────────────────────────────────
        // Scoped: one UoW per request, sharing the same DbContext instance.
        // Application services inject IUnitOfWork — NEVER UnitOfWork directly.
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        // ── Generic Repository (open generic) ──────────────────────────────────
        // Allows injecting IRepository<T> directly in edge cases.
        // Normally accessed via IUnitOfWork.Repository<T>() instead.
        services.AddScoped(typeof(IRepository<>), typeof(GenericRepository<>));

        // ── Specific Repositories ──────────────────────────────────────────────
        // Registered separately so they can be injected directly if ever needed,
        // but the standard approach is always through IUnitOfWork.
        services.AddScoped<IPropertyRepository, PropertyRepository>();
        services.AddScoped<IBookingRepository, BookingRepository>();
        services.AddScoped<IStudentRepository, StudentRepository>();
        services.AddScoped<IOwnerRepository, OwnerRepository>();
        services.AddScoped<IChatRepository, ChatRepository>();
        services.AddScoped<IReviewRepository, ReviewRepository>();
        services.AddScoped<IReportRepository, ReportRepository>();
        services.AddScoped<INotificationRepository, NotificationRepository>();
        services.AddScoped<IFavoriteRepository, FavoriteRepository>();
        services.AddScoped<IUniversityRepository, UniversityRepository>();
        // ── Auth Services ──────────────────────────────────────────────────────────
        services.AddScoped<IJwtTokenService, JwtTokenService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IEmailService, EmailService>();
        // Skanly.Infrastructure/DependencyInjection.cs — أضف السطر ده
        services.AddScoped<IIdentityService, IdentityService>();
        services.AddScoped<IFileStorageService, LocalFileStorageService>();
        // ── Payment Gateways (all registered so factory can resolve by Method) ────────
        services.AddScoped<IPaymentGateway, SimulatedVisaGateway>();
        services.AddScoped<IPaymentGateway, SimulatedMastercardGateway>();
        services.AddScoped<IPaymentGateway, SimulatedVodafoneCashGateway>();
        services.AddScoped<IPaymentGateway, SimulatedInstaPayGateway>();
        services.AddScoped<IPaymentGateway, SimulatedFawryGateway>();
        // Factory resolves the right gateway from the collection above
        services.AddScoped<IPaymentGatewayFactory, PaymentGatewayFactory>();


        return services;
    }
}