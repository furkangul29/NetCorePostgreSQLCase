using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Logging;

namespace WebUI.Services.TokenServices
{
    public class RequireScopeAttribute : ActionFilterAttribute
    {
        private readonly string _requiredScope;

        public RequireScopeAttribute(string requiredScope)
        {
            _requiredScope = requiredScope;
        }

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            var logger = context.HttpContext.RequestServices.GetService(typeof(ILogger<RequireScopeAttribute>)) as ILogger<RequireScopeAttribute>;

            var authorizationHeader = context.HttpContext.Request.Headers["Authorization"].FirstOrDefault();
            var requestUrl = context.HttpContext.Request.Path;
            var requestTime = DateTime.UtcNow; // UTC zamanı al

 
            var ipAddress = context.HttpContext.Connection.RemoteIpAddress?.ToString();
            var userIdentifier = context.HttpContext.User.Identity?.Name ?? "Bilinmeyen Kullanıcı";

            if (string.IsNullOrEmpty(authorizationHeader) || !authorizationHeader.StartsWith("Bearer "))
            {
                logger?.LogWarning($"Yetkisiz erişim denemesi: {userIdentifier} IP: {ipAddress} tarihinde {requestTime} saatinde {requestUrl} adresine erişmeye çalıştı.");
                context.Result = new UnauthorizedResult();
                return;
            }

            var accessToken = authorizationHeader.Substring("Bearer ".Length).Trim();

            var tokenHandler = new JwtSecurityTokenHandler();
            if (tokenHandler.CanReadToken(accessToken))
            {
                var jwtToken = tokenHandler.ReadJwtToken(accessToken);

                var hasScope = jwtToken.Claims.Any(c =>
                    c.Type == "scope" && c.Value == _requiredScope);

                if (!hasScope)
                {
                    logger?.LogWarning($"Erişim reddedildi: {userIdentifier} IP: {ipAddress} tarihinde {requestTime} saatinde {requestUrl} adresine erişmeye çalıştı. Gerekli kapsam: {_requiredScope}.");
                    context.Result = new RedirectToActionResult("Index", "Forbidden", null);
                    return;
                }

                logger?.LogInformation($"Erişim verildi: {userIdentifier} IP: {ipAddress} tarihinde {requestTime} saatinde {requestUrl} adresine erişti.");
            }
            else
            {
                logger?.LogWarning($"Yetkisiz erişim denemesi: {userIdentifier} IP: {ipAddress} tarihinde {requestTime} saatinde {requestUrl} adresine erişmeye çalıştı.");
                context.Result = new UnauthorizedResult();
                return;
            }

            base.OnActionExecuting(context);
        }
    }
}
