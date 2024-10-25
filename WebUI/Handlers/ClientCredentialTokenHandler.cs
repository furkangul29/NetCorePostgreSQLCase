using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using WebUI.DTO.IdentityDtos.LoginDtos;
using WebUI.Services.ClientCredentialTokenServices;

namespace WebUI.Handlers
{
    public class ClientCredentialTokenHandler : DelegatingHandler
    {
        private readonly IClientCredentialTokenService _clientCredentialTokenService;

        public ClientCredentialTokenHandler(IClientCredentialTokenService clientCredentialTokenService)
        {
            _clientCredentialTokenService = clientCredentialTokenService;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            // Kullanıcı adı ve şifreyi nereden alacağınızı belirtmelisiniz.
            var signInDto = new SignInDto
            {
                Username = "admin", // Burada kullanıcı adını belirtin
                Password = "Admin1234!"          // Burada şifreyi belirtin
            };

            // Token'ı almak için DTO'yu kullanıyoruz
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", await _clientCredentialTokenService.GetToken(signInDto));
            var response = await base.SendAsync(request, cancellationToken);

            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                // Burada hataya dair mesaj verilebilir
            }

            return response;
        }
    }
}
