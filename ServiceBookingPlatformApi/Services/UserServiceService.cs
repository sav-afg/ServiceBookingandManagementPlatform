using Microsoft.EntityFrameworkCore;
using ServiceBookingPlatform.Data;
using ServiceBookingPlatform.Models;
using ServiceBookingPlatform.Models.Dtos.Service;
using ServiceBookingPlatform.Services.Common;

namespace ServiceBookingPlatform.Services
{
    public class UserServiceService(AppDbContext Db) : IUserServiceService
    {

        private IQueryable<ServiceDto> GetServiceQuery()
        {
            return Db.Services
                .Select(b => new ServiceDto(
                    b.Id,
                    b.ServiceName,
                    b.ServiceType,
                    b.ServiceDescription
                ));
        }

        public async Task<Result<ServiceDto>> CreateServiceAsync(CreateServiceDto newService)
        {
            // Authorization is handled at controller level with [Authorize(Roles = "Admin")]
            
            // Check if service with the same name already exists
            if (await Db.Services.AnyAsync(s => s.ServiceName == newService.ServiceName))
            {
                return Result<ServiceDto>.Failure("A service with the same name already exists.");
            }

            // Create new service entity
            var service = new Service
            {
                ServiceName = newService.ServiceName,
                ServiceType = newService.ServiceType,
                ServiceDescription = newService.ServiceDescription
            };

            Db.Services.Add(service);
            await Db.SaveChangesAsync();

            var createdService = await GetServiceByIdAsync(service.Id);

            if (createdService == null)
            {
                return Result<ServiceDto>.Failure("Failed to retrieve the created service.");
            }

            return Result<ServiceDto>.Success(createdService, "Service created successfully.");
        }

        public async Task<bool> DeleteServiceAsync(int serviceId)
        {
            var service = Db.Services.FirstOrDefault(b => b.Id == serviceId);

            if (service == null)
                return false;

            Db.Services.Remove(service);
            await Db.SaveChangesAsync();
            return true;
        }

        public async Task<List<ServiceDto>> GetAllServicesAsync()
        {
            return await GetServiceQuery().ToListAsync();
        }

        public async Task<ServiceDto?> GetServiceByIdAsync(int serviceId)
        {
            return await Db.Services
                .Where(s => s.Id == serviceId)
                .Select(s => new ServiceDto(
                    s.Id,
                    s.ServiceName,
                    s.ServiceType,
                    s.ServiceDescription
                ))
                .FirstOrDefaultAsync();
        }

        public async Task<bool> ServiceExistsAsync(int serviceId)
        {
            return await Db.Services.AnyAsync(s => s.Id == serviceId);
        }

        public async Task<Result<ServiceDto?>> UpdateServiceAsync(int serviceId, UpdateServiceDto updatedService)
        {
            // Authorization is handled at controller level with [Authorize(Roles = "Admin")]
            
            var existingService = await Db.Services.FindAsync(serviceId);

            // Check if the service exists
            if (existingService == null)
            {
                return Result<ServiceDto?>.Failure("Service not found.");
            }

            // Update only the fields that are provided in the updatedService DTO
            if (!string.IsNullOrEmpty(updatedService.ServiceName))
                existingService.ServiceName = updatedService.ServiceName;

            if (!string.IsNullOrEmpty(updatedService.ServiceType))
                existingService.ServiceType = updatedService.ServiceType;

            if (!string.IsNullOrEmpty(updatedService.ServiceDescription))
                existingService.ServiceDescription = updatedService.ServiceDescription;


            await Db.SaveChangesAsync();

            return Result<ServiceDto?>.Success(await GetServiceByIdAsync(serviceId), "Service updated successfully.");
        }
    }
}
