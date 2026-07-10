using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Skanly.Application.Common.Mappings;
using Skanly.Application.Interfaces.Services;
using Skanly.Application.Services;
using AutoMapper;
namespace Skanly.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
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

