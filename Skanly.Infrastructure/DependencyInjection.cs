using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

using Skanly.Application.Common.Interfaces;
using Skanly.Application.Common.Interfaces.Repositories;
using Skanly.Application.Features.Auth.Interfaces;
using Skanly.Application.Features.Chatbot.Interfaces;
using Skanly.Application.Features.Maps.Interfaces;
using Skanly.Application.Features.Payments.Interfaces;
using Skanly.Application.Features.Recommendations.Interfaces;
using Skanly.Application.Features.Verification.Interfaces;

using Skanly.Infrastructure.AI;
using Skanly.Infrastructure.ExternalServices.Email;
using Skanly.Infrastructure.ExternalServices.GoogleMaps;
using Skanly.Infrastructure.ExternalServices.Ocr;
using Skanly.Infrastructure.ExternalServices.Payment;
using Skanly.Infrastructure.FileStorage;
using Skanly.Infrastructure.Identity;
using Skanly.Infrastructure.Persistence;
using Skanly.Infrastructure.Persistence.Repositories;
using Skanly.Infrastructure.RealTime;

using Skanly.Application.Features.Contracts.Interfaces;
using Skanly.Infrastructure.Pdf;

namespace Skanly.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // ─────────────────────────────────────────────────────────────
        // DbContext
        // ─────────────────────────────────────────────────────────────
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

        // ─────────────────────────────────────────────────────────────
        // Identity
        // ─────────────────────────────────────────────────────────────
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

        // ─────────────────────────────────────────────────────────────
        // Google Maps
        // ─────────────────────────────────────────────────────────────
        services.Configure<GoogleMapsSettings>(
            configuration.GetSection(GoogleMapsSettings.SectionName));

        services.AddHttpClient<IGoogleMapsService, GoogleMapsService>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(10);
            client.DefaultRequestHeaders.Add("Accept", "application/json");
        });

        // ─────────────────────────────────────────────────────────────
        // Claude Settings
        // ─────────────────────────────────────────────────────────────
        services.Configure<ClaudeClientSettings>(
            configuration.GetSection(ClaudeClientSettings.SectionName));

        // Claude Recommendation Client
        services.AddHttpClient<IClaudeRecommendationClient, ClaudeRecommendationClient>(
            (sp, client) =>
            {
                var settings = sp.GetRequiredService<IOptions<ClaudeClientSettings>>().Value;

                client.BaseAddress = new Uri("https://api.anthropic.com");
                client.DefaultRequestHeaders.Add("x-api-key", settings.ApiKey);
                client.DefaultRequestHeaders.Add("anthropic-version", "2023-06-01");
                client.DefaultRequestHeaders.Add("Accept", "application/json");

                client.Timeout = TimeSpan.FromSeconds(settings.TimeoutSeconds + 5);
            });

        // Claude Chat Client
        services.AddHttpClient<IClaudeChatClient, ClaudeChatClient>(
            (sp, client) =>
            {
                var settings = sp.GetRequiredService<IOptions<ClaudeClientSettings>>().Value;

                client.BaseAddress = new Uri("https://api.anthropic.com");
                client.DefaultRequestHeaders.Add("x-api-key", settings.ApiKey);
                client.DefaultRequestHeaders.Add("anthropic-version", "2023-06-01");
                client.DefaultRequestHeaders.Add("Accept", "application/json");

                client.Timeout = TimeSpan.FromSeconds(settings.TimeoutSeconds + 60);
            });

        // ─────────────────────────────────────────────────────────────
        // Unit Of Work
        // ─────────────────────────────────────────────────────────────
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        // Generic Repository
        services.AddScoped(typeof(IRepository<>), typeof(GenericRepository<>));

        // Repositories
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

        // Identity
        services.AddScoped<IIdentityService, IdentityService>();

        // Auth
        services.AddScoped<IJwtTokenService, JwtTokenService>();
        services.AddScoped<IAuthService, AuthService>();

        // Email
        services.AddScoped<IEmailService, EmailService>();

        // File Storage
        services.AddScoped<IFileStorageService, LocalFileStorageService>();

        // OCR
        services.Configure<OcrSettings>(
            configuration.GetSection(OcrSettings.SectionName));

        services.AddScoped<IOcrService, TesseractOcrService>();

        // Payment
        services.AddScoped<IPaymentGateway, SimulatedVisaGateway>();
        services.AddScoped<IPaymentGateway, SimulatedMastercardGateway>();
        services.AddScoped<IPaymentGateway, SimulatedVodafoneCashGateway>();
        services.AddScoped<IPaymentGateway, SimulatedInstaPayGateway>();
        services.AddScoped<IPaymentGateway, SimulatedFawryGateway>();

        services.AddScoped<IPaymentGatewayFactory, PaymentGatewayFactory>();

        // SignalR Notification
        services.AddScoped<INotificationHub, NotificationHubHelper>();

        services.Configure<ContractSettings>(
    configuration.GetSection(ContractSettings.SectionName));

        services.AddScoped<IPdfContractService, QuestPdfContractService>();

        return services;
    }
}