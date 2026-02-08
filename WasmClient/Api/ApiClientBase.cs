using System.Net.Http.Json;

namespace WasmClient.Api
{
    
    public abstract class ApiClientBase(HttpClient httpClient, ILogger logger)
    {
        
        protected async Task<T?> GetAsync<T>(string endpoint)
        {
            try
            {
                return await httpClient.GetFromJsonAsync<T>(endpoint);
            }
            catch (HttpRequestException ex)
            {
                logger.LogError(ex, "GET request failed: {Endpoint}", endpoint);
                throw;
            }
        }

        protected async Task<TResponse?> PostAsync<TRequest, TResponse>(
            string endpoint, TRequest data)
        {
            try
            {
                var response = await httpClient.PostAsJsonAsync(endpoint, data);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<TResponse>();
            }
            catch (HttpRequestException ex)
            {
                logger.LogError(ex, "POST request failed: {Endpoint}", endpoint);
                throw;
            }
        }
    }
}
