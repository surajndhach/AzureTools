using LoadPerformanceTest.Logging;
using LoadPerformanceTest.Services.Authentication;
using ONE.Models.CSharp;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Tenant = LoadPerformanceTest.Models.Tenant;

namespace LoadPerformanceTest.Services.Facades
{
    public static class TenantFacade
    {
        /// <summary>
        /// Creates the tenants from the list of data provided.
        /// </summary>
        public static async Task<long> CreateTenantsAsync(List<Tenant> data)
        {
            Logger.LogInfo($"Starting tenant creation for {data.Count} tenant(s).");

            var count = 0;
            foreach (var tenant in data)
            {
                var statusCode = await CallCreateTenantApiAsync(tenant.TenantId, tenant.TenantName);
                if (statusCode == HttpStatusCode.Created)
                {
                    count++;
                }
                else
                {
                    Logger.LogWarning($"Tenant '{tenant.TenantId}' was not created. HTTP {(int)statusCode} {statusCode}.");
                }
            }

            Logger.LogInfo($"Tenant creation completed: {count}/{data.Count} succeeded.");
            return count;
        }

        /// <summary>
        /// Sends a request to the Create Tenant API to create a new tenant with the specified ID and name.
        /// </summary>
        public static async Task<HttpStatusCode> CallCreateTenantApiAsync(string tenantId, string name)
        {
            try
            {
                using var client = new HttpClient();
                var url = "https://api-feature-us.aquaticinformatics.net/enterprise/core/v1/tenant";

                var tenant = new ONE.Models.CSharp.Tenant
                {
                    Name = name,
                    Id = tenantId,
                    Culture = "am",
                    EnumTimeZone = EnumTimeZone.TimezoneUtc
                };

                var json = JsonSerializer.Serialize(tenant);
                var request = new HttpRequestMessage(HttpMethod.Post, url);

                var token = await AuthToken.GetAdminTokenAsync();
                if (string.IsNullOrEmpty(token))
                {
                    Logger.LogError($"Skipping tenant creation for '{tenantId}' — failed to acquire auth token.");
                    return HttpStatusCode.Unauthorized;
                }

                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                request.Content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await client.SendAsync(request);

                if (response.StatusCode != HttpStatusCode.Created)
                    Logger.LogWarning($"Unexpected response creating tenant '{tenantId}'. HTTP {(int)response.StatusCode} {response.StatusCode}.");
                else
                    Logger.LogInfo($"Tenant '{tenantId}' created successfully.");

                return response.StatusCode;
            }
            catch (HttpRequestException ex)
            {
                Logger.LogError($"Network error while creating tenant '{tenantId}'.", ex);
                return HttpStatusCode.ServiceUnavailable;
            }
            catch (Exception ex)
            {
                Logger.LogError($"Unexpected error while creating tenant '{tenantId}'.", ex);
                return HttpStatusCode.InternalServerError;
            }
        }

        /// <summary>
        /// Deletes created tenants from the list of data provided.
        /// </summary>
        public static async Task<long> DeleteTenantsAsync(List<Tenant> data)
        {
            Logger.LogInfo($"Starting tenant deletion for {data.Count} tenant(s).");
            var count = 0;
            foreach (var tenant in data)
            {
                var statusCode = await CallTenantDeleteAPIAsync(tenant.TenantId);
                if (statusCode == HttpStatusCode.NoContent)
                {
                    count++;
                }
                else
                {
                    Logger.LogWarning($"Tenant '{tenant.TenantId}' was not deleted. HTTP {(int)statusCode} {statusCode}.");
                }
            }

            Logger.LogInfo($"Tenant deletion completed: {count}/{data.Count} succeeded.");
            return count;
        }

        /// <summary>
        /// Sends an asynchronous HTTP DELETE request to remove a tenant by ID.
        /// </summary>
        public static async Task<HttpStatusCode> CallTenantDeleteAPIAsync(string tenantId)
        {
            try
            {
                using var client = new HttpClient();
                var url = $"https://api-feature-us.aquaticinformatics.net/enterprise/core/v1/tenant/{tenantId}";
                var request = new HttpRequestMessage(HttpMethod.Delete, url);

                var token = await AuthToken.GetAdminTokenAsync();
                if (string.IsNullOrEmpty(token))
                {
                    Logger.LogError($"Skipping tenant deletion for '{tenantId}' — failed to acquire auth token.");
                    return HttpStatusCode.Unauthorized;
                }

                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                var response = await client.SendAsync(request);

                if (response.StatusCode != HttpStatusCode.NoContent)
                    Logger.LogWarning($"Unexpected response deleting tenant '{tenantId}'. HTTP {(int)response.StatusCode} {response.StatusCode}.");
                else
                    Logger.LogInfo($"Tenant '{tenantId}' deleted successfully.");

                return response.StatusCode;
            }
            catch (HttpRequestException ex)
            {
                Logger.LogError($"Network error while deleting tenant '{tenantId}'.", ex);
                return HttpStatusCode.ServiceUnavailable;
            }
            catch (Exception ex)
            {
                Logger.LogError($"Unexpected error while deleting tenant '{tenantId}'.", ex);
                return HttpStatusCode.InternalServerError;
            }
        }
    }
}
