using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Configuration;

namespace LoadPerformanceTest
{
    internal static class AuthToken
    {
        private static readonly IConfigurationSection _authConfig;

        static AuthToken()
        {
            var config = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: false)
                .Build();

            _authConfig = config.GetSection("AdminAuth");
        }

        internal static async Task<string?> GetAdminTokenAsync()
        {
            try
            {
                using var client = new HttpClient();

                var request = new HttpRequestMessage(HttpMethod.Post, _authConfig["TokenEndpoint"]);

                var body = new Dictionary<string, string>
            {
                { "grant_type", _authConfig["GrantType"] ?? "password" },
                { "username", _authConfig["Username"] ?? string.Empty },
                { "password", _authConfig["Password"] ?? string.Empty },
                { "client_id", _authConfig["ClientId"] ?? string.Empty },
                { "client_secret", _authConfig["ClientSecret"] ?? string.Empty },
                { "scope", _authConfig["Scope"] ?? string.Empty }
            };

                request.Content = new FormUrlEncodedContent(body);

                var response = await client.SendAsync(request);

                if (!response.IsSuccessStatusCode)
                {
                    Logger.LogWarning($"Token request failed. HTTP {(int)response.StatusCode} {response.StatusCode}.");
                    return null;
                }

                var content = await response.Content.ReadAsStringAsync();

                var tokenResponse = JsonSerializer.Deserialize<TokenResponse>(
                    content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (tokenResponse?.AccessToken is null or "")
                {
                    Logger.LogWarning("Token response was successful but access_token is null or empty.");
                    return null;
                }
                return tokenResponse.AccessToken;
            }
            catch (HttpRequestException ex)
            {
                Logger.LogError("Network error while requesting admin token.", ex);
                return null;
            }
            catch (JsonException ex)
            {
                Logger.LogError("Failed to deserialize token response.", ex);
                return null;
            }
            catch (Exception ex)
            {
                Logger.LogError("Unexpected error while requesting admin token.", ex);
                return null;
            }
        }

        public class TokenResponse
        {
            [JsonPropertyName("access_token")]
            public string AccessToken { get; set; } = string.Empty;
            [JsonPropertyName("expires_in")]
            public int ExpiresIn { get; set; }
            [JsonPropertyName("token_type")]
            public string TokenType { get; set; } = string.Empty;
            [JsonPropertyName("scope")]
            public string Scope { get; set; } = string.Empty;
        }
    }
}
