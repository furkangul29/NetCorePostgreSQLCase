using System.Net;

namespace WebUI.Services.TokenServices
{
    public class TokenCheckMiddleware
    {
        private readonly RequestDelegate _next;

        public TokenCheckMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            if (context.Request.Path.StartsWithSegments("/Customer") ||
                context.Request.Path.StartsWithSegments("/AnotherProtectedController"))
            {
                var token = context.Session.GetString("Token");

                // Eğer token yoksa
                if (string.IsNullOrEmpty(token))
                {
                    // 401 Unauthorized yanıtı ver
                    context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                    await context.Response.WriteAsync("Unauthorized: Token is missing.");
                    return;
                }

                // Eğer token varsa, başlıkta Authorization ekleyebiliriz
                context.Request.Headers["Authorization"] = $"Bearer {token}";
            }

            await _next(context);
        }
    }
}
