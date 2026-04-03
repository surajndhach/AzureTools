using System.Text.Json;
using System.Text.Json.Serialization;

namespace LoadPerformanceTest
{
    internal static class AuthToken
    {
        internal static async Task<string?> GetAdminTokenAsync()
        {
            using var client = new HttpClient();

            var request = new HttpRequestMessage(HttpMethod.Post,"https://api-feature-us.aquaticinformatics.net/connect/token");

            var body = new Dictionary<string, string>
            {
                { "grant_type", "password" },
                { "username", "Abhi01" },
                { "password", "Password3637*" },
                { "client_id", "VSTestClient" },
                { "client_secret", "0CCBB786-9412-4088-BC16-78D3A10158B7" },
                { "scope", "FFAccessAPI openid" }
            };

            request.Content = new FormUrlEncodedContent(body);

            var response = await client.SendAsync(request);
           
            if (!response.IsSuccessStatusCode)
                return null;

            var content = await response.Content.ReadAsStringAsync();

            var tokenResponse = JsonSerializer.Deserialize<TokenResponse>(
                content,new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            return tokenResponse?.AccessToken;
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
