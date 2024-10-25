using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Text;
using WebUI.DTO.IdentityDtos.LoginDtos;
using WebUI.Services.ClientCredentialTokenServices;

namespace WebUI.Services.RegistrationTokenServices
{
    public class RegistrationTokenService : IRegistrationTokenService
    {
        private readonly IClientCredentialTokenService _clientCredentialTokenService;
        private readonly IConfiguration _configuration;

        public RegistrationTokenService(
            IClientCredentialTokenService clientCredentialTokenService,
            IConfiguration configuration)
        {
            _clientCredentialTokenService = clientCredentialTokenService;
            _configuration = configuration;
        }

        public async Task<string> GenerateRegistrationToken(int roleId)
        {
            var signInDto = new SignInDto
            {
                Username = "admin", // Burada kullanıcı adını belirtin
                Password = "Admin1234!"          // Burada şifreyi belirtin
            };
            var accessToken = await _clientCredentialTokenService.GetToken(signInDto);

            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]);

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Claims = new Dictionary<string, object>
            {
                { "roleId", roleId },
                { "purpose", "registration" },
                { "access_token", accessToken },
                { "scope", "users.write" }
            },
                Expires = DateTime.UtcNow.AddMinutes(15),
                Issuer = "http://localhost:3001",
                SigningCredentials = new SigningCredentials(
                    new SymmetricSecurityKey(key),
                    SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }

        public bool ValidateRegistrationToken(string token, out int roleId, out string accessToken)
        {
            roleId = 0;
            accessToken = null;

            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]);

            try
            {
                var validationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = true,
                    ValidIssuer = "http://localhost:3001",
                    ValidateAudience = false,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero
                };

                tokenHandler.ValidateToken(token, validationParameters, out SecurityToken validatedToken);

                var jwtToken = (JwtSecurityToken)validatedToken;

                if (jwtToken.Claims.FirstOrDefault(x => x.Type == "purpose")?.Value != "registration")
                    return false;

                if (!jwtToken.Claims.Any(x => x.Type == "scope" && x.Value == "users.write"))
                    return false;

                roleId = int.Parse(jwtToken.Claims.First(x => x.Type == "roleId").Value);
                accessToken = jwtToken.Claims.First(x => x.Type == "access_token").Value;
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
