using ServiceBookingPlatform.Models.Dtos.Service;
using ServiceBookingPlatform.Services.Common;

namespace ServiceBookingPlatform.Services
{
    public interface IUserServiceService
    {
        public Task<bool> ServiceExistsAsync(int serviceId);

        Task<List<ServiceDto>> GetAllServicesAsync();

        Task<ServiceDto?> GetServiceByIdAsync(int serviceId);

        Task<Result<ServiceDto>> CreateServiceAsync(CreateServiceDto newService);

        Task<Result<ServiceDto?>> UpdateServiceAsync(int serviceId, UpdateServiceDto updatedService);

        Task<bool> DeleteServiceAsync(int serviceId);

    }
}
