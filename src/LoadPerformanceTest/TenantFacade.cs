using LoadPerformanceTest.Models;
using ONE.Models.CSharp;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Tenant = LoadPerformanceTest.Models.Tenant;

namespace LoadPerformanceTest
{
    public static class TenantFacade
    {
        /// <summary>
        /// creates the tenants from the list of data provided;
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public static async Task<long> CreateTenantsAsync(List<Tenant> data) {

            var count = 0;
            foreach(var tenant in data)
            {
                var statusCode = await CallCreateTenantApiAsync(tenant.TenantId, tenant.TenantName);
                if (statusCode == HttpStatusCode.Created)
                {
                    count++;
                }
            }
            return count;
        }

        /// <summary>
        /// Sends a request to the Create Tenant API to create a new tenant with the specified ID and name.
        /// </summary>
        /// <param name="tenantId">The unique identifier for the tenant.</param>
        /// <param name="name">The name of the tenant.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        public static async Task<HttpStatusCode> CallCreateTenantApiAsync(string tenantId, string name)
        {
            using var client = new HttpClient();
            var url = "https://api-feature-us.aquaticinformatics.net/enterprise/core/v1/tenant";

            //create Tenant object
            var tenant = new ONE.Models.CSharp.Tenant
            {
                Name = name,
                Id = tenantId,
                Culture = "am",
                EnumTimeZone = EnumTimeZone.TimezoneUtc
            };

            var json = JsonSerializer.Serialize(tenant);
            var request = new HttpRequestMessage(HttpMethod.Post, url);

            //Get Token
            var token = await AuthToken.GetAdminTokenAsync();
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            request.Content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await client.SendAsync(request);

           return response.StatusCode;
        }

        /// <summary>
        /// Deletes created Tenants from the list of data provided by calling the Delete Tenant API for each tenant ID.
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public static async Task<long> DeleteTenantsAsync(List<Tenant> data)
        {
            var count = 0;
            foreach (var tenant in data)
            {
                var statusCode = await CallTenantDeleteAPIAsync(tenant.TenantId);
                if (statusCode == HttpStatusCode.NoContent)
                {
                    count++;
                }   
            }
            return count;
        }


        /// <summary>
        /// Sends an asynchronous HTTP DELETE request to remove a tenant by ID from the Aquatic Informatics Enterprise
        /// Core API.
        /// </summary>
        /// <param name="tenantId">The unique identifier of the tenant to delete.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        public static async Task<HttpStatusCode> CallTenantDeleteAPIAsync(string tenantId)
        {
            //create Request 
            using var client = new HttpClient();
            var url = $"https://api-feature-us.aquaticinformatics.net/enterprise/core/v1/tenant/{tenantId}";
            var request = new HttpRequestMessage(HttpMethod.Delete, url);

            //Token
            var token = await AuthToken.GetAdminTokenAsync();
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            var response = await client.SendAsync(request);

            //response
           return response.StatusCode;
        }
    }
}
