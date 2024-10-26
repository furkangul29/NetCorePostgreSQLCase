using Microsoft.AspNetCore.Authentication;
using System.Security.Claims;

namespace WebUI.Services.TokenServices
{
    public class AddNameClaimTransformation : IClaimsTransformation
    {
        public Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
        {
            // Eğer Identity.Name null ise, ClaimTypes.Name değerini Identity.Name olarak ayarla
            var identity = (ClaimsIdentity)principal.Identity;

            if (identity != null && string.IsNullOrEmpty(identity.Name))
            {
                var nameClaim = identity.FindFirst(ClaimTypes.Name);
                if (nameClaim != null)
                {
                    // Identity.Name boşsa, ClaimTypes.Name ile güncelle
                    identity.AddClaim(new Claim(identity.NameClaimType, nameClaim.Value));
                }
            }

            return Task.FromResult(principal);
        }
    }
}
