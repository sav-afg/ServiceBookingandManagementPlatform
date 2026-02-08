using ServiceBookingPlatform.Models.Dtos.Service;
using System.Net.Http.Json;

namespace WasmClient.Api
{
    public interface IUserServiceApiClient
    {
        Task<List<ServiceDto>> GetAllServicesAsync();
        Task<ServiceDto?> GetServiceByIdAsync(int id);
        Task<ServiceDto> CreateServiceAsync(CreateServiceDto service);
        Task<ServiceDto> UpdateServiceAsync(int id, UpdateServiceDto service);
        Task DeleteServiceAsync(int id);
    }

    public class UserServiceApiClient(HttpClient httpClient, ILogger<UserServiceApiClient> logger)
        : ApiClientBase(httpClient, logger), IUserServiceApiClient
    {
        public async Task<List<ServiceDto>> GetAllServicesAsync()
            => await GetAsync<List<ServiceDto>>("api/userservice") ?? new List<ServiceDto>();

        public async Task<ServiceDto?> GetServiceByIdAsync(int id)
            => await GetAsync<ServiceDto>($"api/userservice/{id}");

        public async Task<ServiceDto> CreateServiceAsync(CreateServiceDto service)
            => await PostAsync<CreateServiceDto, ServiceDto>("api/userservice", service)
                ?? throw new InvalidOperationException("Service creation failed");

        public async Task<ServiceDto> UpdateServiceAsync(int id, UpdateServiceDto service)
        {
            try
            {
                var response = await httpClient.PutAsJsonAsync($"api/userservice/{id}", service);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<ServiceDto>()
                    ?? throw new InvalidOperationException("Service update failed");
            }
            catch (HttpRequestException ex)
            {
                logger.LogError(ex, "PUT request failed: api/userservice/{Id}", id);
                throw;
            }
        }

        public async Task DeleteServiceAsync(int id)
        {
            try
            {
                var response = await httpClient.DeleteAsync($"api/userservice/{id}");
                response.EnsureSuccessStatusCode();
            }
            catch (HttpRequestException ex)
            {
                logger.LogError(ex, "DELETE request failed: api/userservice/{Id}", id);
                throw;
            }
        }
    }
}
