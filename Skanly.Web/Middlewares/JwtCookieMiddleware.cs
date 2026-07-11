// Skanly.Web/Middlewares/JwtCookieMiddleware.cs
namespace Skanly.Web.Middlewares;

/// <summary>
/// Reads the JWT from the HttpOnly cookie and writes it into the
/// Authorization header so ASP.NET's JWT bearer middleware validates it
/// transparently — the cookie approach prevents XSS token theft.
/// </summary>
public class JwtCookieMiddleware
{
    private readonly RequestDelegate _next;

    public JwtCookieMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var token = context.Request.Cookies["skanly_access_token"];

        if (!string.IsNullOrEmpty(token) &&
            !context.Request.Headers.ContainsKey("Authorization"))
        {
            context.Request.Headers.Append("Authorization", $"Bearer {token}");
        }

        await _next(context);
    }
}