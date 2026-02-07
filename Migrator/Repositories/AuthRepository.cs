using System.Diagnostics;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.WebUtilities;
using Migrator.Core;
using Migrator.HTTPClient;

namespace Migrator.Repositories
{
    public class AuthRepository
    {
        readonly Config _config;

        private string? _bearer { get; set; }

        public AuthRepository(Config config, MyInterceptHttpClient httpClient)
        {
            _config = config;
        }

        private static IEnumerable<KeyValuePair<string, string>> GetReqParams(Config config)
        {
            return new List<KeyValuePair<string, string>>
            {
                new("grant_type", "client_credentials"),
                new("client_id", config.ClientId),
                new("client_secret", config.ClientSecret),
            };
        }

        public async Task<string> GetBearerTokenFromCallbackData(string code)
        {
            var timeoutMs = _config.SpotifyRequestTimeoutInMs;
            using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(timeoutMs));
            IEnumerable<KeyValuePair<string, string>> formBody = new List<
                KeyValuePair<string, string>
            >
            {
                new("code", code),
                new("redirect_uri", _config.SpotifyRedirectUri),
                new("grant_type", "authorization_code"),
            };

            var httpClient = new MyInterceptHttpClient();
            var reqContent = new FormUrlEncodedContent(formBody);

            string credentials = $"{_config.ClientId}:{_config.ClientSecret}";
            string base64Credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes(credentials));

            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
                "Basic",
                base64Credentials
            );
            using HttpResponseMessage authResponse = await httpClient.PostAsync(
                _config.SpotifyTokenUri,
                reqContent,
                cts.Token
            );

            string jsonResponse = await authResponse.Content.ReadAsStringAsync();

            using JsonDocument document = JsonDocument.Parse(jsonResponse);
            var root = document.RootElement;
            var accessToken = root.GetProperty("access_token").GetString();
            var tokenType = root.GetProperty("token_type").GetString();
            var scope = root.GetProperty("scope").GetString();

            Trace.Assert(tokenType == "Bearer", "Token type must be Bearer");
            _bearer = accessToken ?? throw new Exception("Bad bearer");

            return _bearer;
        }

        public Task<string> RotateToken(string oldToken)
        {
            throw new NotImplementedException();
        }

        public string GetLoginRedirectUrl()
        {
            // var state = generateRandomString(16);
            // res.cookie(stateKey, state);
            var spotifyRedirectUri = _config.SpotifyLoginUri;
            var queryParams = new Dictionary<string, string?>()
            {
                ["response_type"] = "code",
                ["client_id"] = _config.ClientId,
                ["scope"] = _config.SpotifyOAuthScope,
                ["redirect_uri"] = _config.SpotifyRedirectUri,
                // ["state"] = "213" // TODO: random state?
            };

            string fullUri = QueryHelpers.AddQueryString(spotifyRedirectUri, queryParams);

            return fullUri;
        }
    }
}
