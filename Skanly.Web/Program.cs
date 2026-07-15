using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Localization;
using Microsoft.IdentityModel.Tokens;
using QuestPDF.Infrastructure;
using Skanly.Application;
using Skanly.Application.Common.Interfaces;
using Skanly.Application.Features.Localization.Services;
using Skanly.Infrastructure;
using Skanly.Infrastructure.ExternalServices.Email;
using Skanly.Infrastructure.Identity;
using Skanly.Infrastructure.Pdf;
using Skanly.Infrastructure.Persistence.Seed;
using Skanly.Infrastructure.RealTime;
using Skanly.Infrastructure.RealTime.Hubs;
using Skanly.Web.Extensions;
using Skanly.Web.Middlewares;
using System.Globalization;
using Skanly.Web.Resources;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// ── Localization ──────────────────────────────────────────────────────────────
builder.Services.AddLocalization(options =>
    options.ResourcesPath = "Resources");

// ── Core MVC ──────────────────────────────────────────────────────────────────
builder.Services.AddControllersWithViews()
    .AddViewLocalization(
        Microsoft.AspNetCore.Mvc.Razor.LanguageViewLocationExpanderFormat
            .Suffix)
    .AddDataAnnotationsLocalization(options =>
    {
        options.DataAnnotationLocalizerProvider = (type, factory) =>
            factory.Create(typeof(SharedResource));
    });

// 2. IHttpContextAccessor (needed by LocalizationService)
builder.Services.AddHttpContextAccessor();

// 3. ILocalizationService
builder.Services.AddScoped<ILocalizationService, LocalizationService>();
// 4. Supported cultures
var cultures = new[] { new CultureInfo("en"), new CultureInfo("ar") };

builder.Services.Configure<RequestLocalizationOptions>(opts =>
{
    opts.DefaultRequestCulture = new RequestCulture("en");
    opts.SupportedCultures = cultures;
    opts.SupportedUICultures = cultures;
    opts.RequestCultureProviders = new List<IRequestCultureProvider>
    {
        new CookieRequestCultureProvider
        {
            CookieName = ".Skanly.Culture"
        },
        new QueryStringRequestCultureProvider(),
        new AcceptLanguageHeaderRequestCultureProvider()
    };
});


// ── Application + Infrastructure ──────────────────────────────────────────────
builder.Services.AddApplicationServices();
builder.Services.AddInfrastructureServices(builder.Configuration);

// ── Settings ──────────────────────────────────────────────────────────────────
var jwtSettings = builder.Configuration
    .GetSection(JwtSettings.SectionName)
    .Get<JwtSettings>()!;

builder.Services.Configure<JwtSettings>(
    builder.Configuration.GetSection(JwtSettings.SectionName));

builder.Services.Configure<EmailSettings>(
    builder.Configuration.GetSection(EmailSettings.SectionName));

// ── JWT Authentication ────────────────────────────────────────────────────────
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings.Issuer,
        ValidAudience = jwtSettings.Audience,
        IssuerSigningKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(jwtSettings.SecretKey)),
        ClockSkew = TimeSpan.Zero
    };

    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            var accessToken = context.Request.Query["access_token"];
            var path = context.HttpContext.Request.Path;

            if (!string.IsNullOrEmpty(accessToken) &&
                (path.StartsWithSegments("/hubs/chat") ||
                 path.StartsWithSegments("/hubs/notification")))
            {
                context.Token = accessToken;
            }

            return Task.CompletedTask;
        }
    };
});

// ── Authorization ─────────────────────────────────────────────────────────────
builder.Services.AddSkanlyAuthorization();

// ── SignalR ───────────────────────────────────────────────────────────────────
builder.Services.AddSignalR(options =>
{
    options.MaximumReceiveMessageSize = 10 * 1024 * 1024;
    options.ClientTimeoutInterval = TimeSpan.FromSeconds(60);
    options.KeepAliveInterval = TimeSpan.FromSeconds(15);
    options.HandshakeTimeout = TimeSpan.FromSeconds(15);
    options.EnableDetailedErrors = builder.Environment.IsDevelopment();
});

// ── Custom Services ───────────────────────────────────────────────────────────
builder.Services.AddSingleton<ConnectionTracker>();
builder.Services.AddScoped<NotificationHubHelper>();

// ── Localization Services ────────────────────────────────────────────────────
var supportedCultures = new[]
{
    new CultureInfo("en"),
    new CultureInfo("ar")
};

builder.Services.Configure<RequestLocalizationOptions>(options =>
{
    options.DefaultRequestCulture = new RequestCulture("en");
    options.SupportedCultures = supportedCultures;
    options.SupportedUICultures = supportedCultures;

    options.RequestCultureProviders = new List<IRequestCultureProvider>
    {
        new CookieRequestCultureProvider
        {
            CookieName = ".Skanly.Culture"
        },
        new QueryStringRequestCultureProvider(),
        new AcceptLanguageHeaderRequestCultureProvider()
    };
});

builder.Services.AddScoped<ILocalizationService, LocalizationService>();

// ── Anti-forgery ──────────────────────────────────────────────────────────────
builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = "X-XSRF-TOKEN";
});

var app = builder.Build();

// Register QuestPDF Community license
QuestPDF.Settings.License = LicenseType.Community;
ContractFonts.Register();

// ── Seed Database ─────────────────────────────────────────────────────────────
using (var scope = app.Services.CreateScope())
{
    await DbInitializer.SeedAsync(scope.ServiceProvider);
}

// ── Middleware Pipeline ───────────────────────────────────────────────────────
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles(new StaticFileOptions
{
    OnPrepareResponse = ctx =>
    {
        if (ctx.File.Name.EndsWith(".css", StringComparison.OrdinalIgnoreCase) ||
            ctx.File.Name.EndsWith(".js", StringComparison.OrdinalIgnoreCase))
        {
            ctx.Context.Response.Headers["Cache-Control"] =
                "public,max-age=86400";
        }
    }
});

app.UseRouting();

// ── Localization Middleware ───────────────────────────────────────────────────
app.UseRequestLocalization();

app.UseMiddleware<JwtCookieMiddleware>();

app.UseAuthentication();
app.UseAuthorization();

// ── Routes ────────────────────────────────────────────────────────────────────
app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Dashboard}/{action=Index}/{id?}");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// ── SignalR Hubs ──────────────────────────────────────────────────────────────
app.MapHub<ChatHub>("/hubs/chat");
app.MapHub<NotificationHub>("/hubs/notification");

// Shortcut route for /Owner
app.MapGet("/Owner", context =>
{
    context.Response.Redirect("/Owner/Dashboard");
    return Task.CompletedTask;
});

// Shortcut route for /Student
app.MapGet("/Student", context =>
{
    context.Response.Redirect("/Student/Dashboard");
    return Task.CompletedTask;
});

app.Run();