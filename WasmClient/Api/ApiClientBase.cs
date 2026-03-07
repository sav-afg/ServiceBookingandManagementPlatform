using System.Net;
using System.Net.Http.Json;

namespace WasmClient.Api
{
    public abstract class ApiClientBase(HttpClient httpClient, ILogger logger)
    {
        // Sends a GET request to the specified endpoint and returns the deserialized response.
        protected async Task<T?> GetAsync<T>(string endpoint)
        {
            var response = await httpClient.GetAsync(endpoint);

            if (response.StatusCode == HttpStatusCode.NotFound)
                return default;

            if (!response.IsSuccessStatusCode)
            {
                logger.LogError("GET request failed with {StatusCode}", (int)response.StatusCode);
                response.EnsureSuccessStatusCode();
            }

            return await response.Content.ReadFromJsonAsync<T>();
        }

        // Sends a POST request with the specified data to the endpoint and returns the deserialized response.
        protected async Task<TResponse?> PostAsync<TRequest, TResponse>(
            string endpoint, TRequest data)
        {
            var response = await httpClient.PostAsJsonAsync(endpoint, data);

            if (!response.IsSuccessStatusCode)
            {
                logger.LogError("POST request failed with {StatusCode}", (int)response.StatusCode);
                response.EnsureSuccessStatusCode();
            }

            return await response.Content.ReadFromJsonAsync<TResponse>();
        }

        // Sends a PATCH request with the specified data to the endpoint and returns the deserialized response.
        protected async Task<TResponse?> PatchAsync<TRequest, TResponse>(
            string endpoint, TRequest data)
        {
            var response = await httpClient.PatchAsJsonAsync(endpoint, data);

            if (!response.IsSuccessStatusCode)
            {
                logger.LogError("PATCH request failed with {StatusCode}", (int)response.StatusCode);
                response.EnsureSuccessStatusCode();
            }

            return await response.Content.ReadFromJsonAsync<TResponse>();
        }

        // Sends a DELETE request to the specified endpoint.
        protected async Task DeleteAsync(string endpoint)
        {
            var response = await httpClient.DeleteAsync(endpoint);

            if (!response.IsSuccessStatusCode)
            {
                logger.LogError("DELETE request failed with {StatusCode}", (int)response.StatusCode);
                response.EnsureSuccessStatusCode();
            }
        }
    }
}
