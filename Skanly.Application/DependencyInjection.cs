using AutoMapper;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Skanly.Application.Common.Interfaces;
using Skanly.Application.Common.Mappings;
using Skanly.Application.Features.Universities.Interfaces;
using Skanly.Application.Features.Universities.Services;
using Skanly.Application.Features.Payments.Services;
using Skanly.Application.Interfaces.Services;
using Skanly.Application.Features.Payments.Interfaces;
using Skanly.Application.Services;
namespace Skanly.Application;

using Skanly.Application.Features.Bookings.Interfaces;
using Skanly.Application.Features.Bookings.Services;

using Skanly.Application.Features.Owners.Interfaces;
using Skanly.Application.Features.Owners.Services;

using Skanly.Application.Features.Properties.Interfaces;
using Skanly.Application.Features.Properties.Services;

using Skanly.Application.Features.Students.Interfaces;
using Skanly.Application.Features.Students.Services;

public static class DependencyInjection
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
     

        // FluentValidation — scans all validators in this assembly
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);

        // ── University ─────────────────────────────────────────────────────────
        services.AddScoped<IUniversityService, UniversityService>();

        // (Parts 9–24 will register their services here)

        services.AddAutoMapper(cfg => { }, typeof(MappingProfile).Assembly);
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);

        services.AddScoped<IStudentService, StudentService>();
        services.AddScoped<IOwnerService, OwnerService>();
        services.AddScoped<IPropertyService, PropertyService>();
        services.AddScoped<IBookingService, BookingService>();
        services.AddScoped<IPaymentService, PaymentService>();
        services.AddScoped<IFavoriteService, FavoriteService>();
        services.AddScoped<IChatService, ChatService>();
        services.AddScoped<IReviewService, ReviewService>();
        services.AddScoped<IReportService, ReportService>();
        services.AddScoped<INotificationService, NotificationService>();
        services.AddScoped<IAnalyticsService, AnalyticsService>();
       

        return services;
    }
}

