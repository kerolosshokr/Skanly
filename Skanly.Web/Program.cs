// Skanly.Web/Program.cs
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Skanly.Application;
using Skanly.Infrastructure;
using Skanly.Infrastructure.ExternalServices.Email;
using Skanly.Infrastructure.Identity;
using Skanly.Infrastructure.Persistence.Seed;
using Skanly.Web.Extensions;
using Skanly.Web.Hubs;
using Skanly.Web.Middlewares;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// ── Core MVC ──────────────────────────────────────────────────────────────────
builder.Services.AddControllersWithViews();

// ── Application + Infrastructure (Parts 5-6 DI) ───────────────────────────────
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
            ClockSkew = TimeSpan.Zero     // Strict expiry — no 5-min grace window
        };

        // Allow JWT from SignalR query string (for ChatHub / NotificationHub)
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

// ── Authorization Policies ────────────────────────────────────────────────────
builder.Services.AddSkanlyAuthorization();

// ── SignalR ───────────────────────────────────────────────────────────────────
builder.Services.AddSignalR();

// ── Anti-forgery ──────────────────────────────────────────────────────────────
builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = "X-XSRF-TOKEN";
});

var app = builder.Build();

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
app.UseStaticFiles();
app.UseRouting();

// Cookie → Authorization header bridge (before UseAuthentication)
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

app.MapHub<ChatHub>("/hubs/chat");
app.MapHub<NotificationHub>("/hubs/notification");

app.Run();