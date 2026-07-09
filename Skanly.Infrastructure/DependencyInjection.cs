using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Skanly.Application.Common.Interfaces;
using Skanly.Application.Interfaces.Services;
using Skanly.Application.Services;
using Skanly.Infrastructure.ExternalServices;
using Skanly.Infrastructure.ExternalServices.AiChatbot;
using Skanly.Infrastructure.FileStorage;
using Skanly.Infrastructure.Identity;
using Skanly.Infrastructure.Persistence;
using Skanly.Infrastructure.Persistence.Repositories;

namespace Skanly.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(
        this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));

        services.AddIdentity<ApplicationUser, IdentityRole>(options =>
        {
            options.Password.RequiredLength = 8;
            options.User.RequireUniqueEmail = true;
        })
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddDefaultTokenProviders();

        services.AddScoped(typeof(IRepository<>), typeof(GenericRepository<>));
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        services.AddScoped<IJwtTokenService, JwtTokenService>();
        services.AddScoped<IGoogleMapsService, GoogleMapsService>();
        services.AddScoped<IOcrService, OcrService>();
        services.AddScoped<IAiRecommendationService, AiRecommendationService>();
        services.AddScoped<IAiChatbotService, AiChatbotService>();
        services.AddScoped<IPdfContractService, PdfContractService>();
        services.AddScoped<IFileStorageService, LocalFileStorageService>();

        return services;
    }
}