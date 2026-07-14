
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Skanly.Application.Common.Mappings;
using Skanly.Application.Features.Bookings.Interfaces;
using Skanly.Application.Features.Bookings.Services;
using Skanly.Application.Features.Chat.Interfaces;
using Skanly.Application.Features.Chat.Services;
using Skanly.Application.Features.Chatbot.Interfaces;
using Skanly.Application.Features.Chatbot.Services;
using Skanly.Application.Features.Favorites.Interfaces;
using Skanly.Application.Features.Favorites.Services;
using Skanly.Application.Features.Notifications.Interfaces;
using Skanly.Application.Features.Notifications.Services;
using Skanly.Application.Features.Owners.Interfaces;
using Skanly.Application.Features.Owners.Services;
using Skanly.Application.Features.Payments.Interfaces;
using Skanly.Application.Features.Payments.Services;
using Skanly.Application.Features.Properties.Interfaces;
using Skanly.Application.Features.Properties.Services;
using Skanly.Application.Features.Recommendations.Interfaces;
using Skanly.Application.Features.Recommendations.Services;
using Skanly.Application.Features.Reports.Interfaces;
using Skanly.Application.Features.Reports.Services;
using Skanly.Application.Features.Reviews.Interfaces;
using Skanly.Application.Features.Reviews.Services;
using Skanly.Application.Features.Students.Interfaces;
using Skanly.Application.Features.Students.Services;
using Skanly.Application.Features.Universities.Interfaces;
using Skanly.Application.Features.Universities.Services;
using Skanly.Application.Features.Verification.Interfaces;
using Skanly.Application.Features.Verification.Services;
using Skanly.Application.Interfaces.Services;
using Skanly.Application.Services;
namespace Skanly.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
     

        // FluentValidation — scans all validators in this assembly
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);

        // ── University ─────────────────────────────────────────────────────────
        services.AddScoped<IUniversityService, UniversityService>();

    

        services.AddAutoMapper(cfg => { }, typeof(MappingProfile).Assembly);
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);

        services.AddScoped<IStudentService, StudentService>();
        services.AddScoped<IOwnerService, OwnerService>();
        services.AddScoped<IPropertyService, PropertyService>();
        services.AddScoped<IBookingService, BookingService>();
        services.AddScoped<IPaymentService, PaymentService>();
        services.AddScoped<IChatService, ChatService>();
        services.AddScoped<IReviewService, ReviewService>();
        services.AddScoped<IReportService, ReportService>();
        services.AddScoped<IFavoriteService, FavoriteService>();
        services.AddScoped<IAnalyticsService, AnalyticsService>();
        services.AddScoped<INotificationService, NotificationService>();
        services.AddScoped<IVerificationService, VerificationService>();
        // Skanly.Application/DependencyInjection.cs  (add)
        services.AddScoped<IRecommendationService, RecommendationService>();
        services.AddScoped<StudentPreferenceAnalyzer>();
        services.AddMemoryCache();  // if not already registered
        services.AddScoped<IChatbotService, ChatbotService>();
        services.AddScoped<ChatbotIntentRouter>();




        return services;
    }
}

