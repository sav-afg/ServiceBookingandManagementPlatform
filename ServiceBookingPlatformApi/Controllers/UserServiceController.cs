using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ServiceBookingPlatform.Models.Dtos.Service;
using ServiceBookingPlatform.Services;
using System.Security.Claims;

namespace ServiceBookingPlatform.Controllers
{
    [Route("api/[controller]")]
    [ApiController]

    public class UserServiceController(IUserServiceService Service, ILogger<UserServiceController> logger) : ControllerBase
    {
        private string ActorId => User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "unknown";
        private string ActorName => User.FindFirst(ClaimTypes.Name)?.Value ?? "unknown";
        private string ActorRole => User.FindFirst(ClaimTypes.Role)?.Value ?? "unknown";

        [HttpGet]
        public async Task<ActionResult<List<ServiceDto>>> GetAllServices()
        {
            var services = await Service.GetAllServicesAsync();

            if (services.Count == 0)
            {
                logger.LogDebug("GetAllServices: No services found");
                return NotFound("No services found.");
            }

            logger.LogDebug("GetAllServices: Returned {Count} service(s)", services.Count);
            return Ok(services);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<ServiceDto>> GetServiceById(int id)
        {
            var service = await Service.GetServiceByIdAsync(id);

            if (service is null)
            {
                logger.LogDebug("GetServiceById: Service {ServiceId} not found", id);
                return NotFound($"Service with ID {id} was not found");
            }

            logger.LogDebug("GetServiceById: Service {ServiceId} retrieved", id);
            return Ok(service);
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<ServiceDto>> AddService(CreateServiceDto service)
        {
            var result = await Service.CreateServiceAsync(service);

            if (!result.IsSuccess || result.Data is null)
            {
                logger.LogWarning("AddService: Failed by {ActorName} (ID: {ActorId}) (Role: {ActorRole}). Reason: {Message}",
                    ActorName, ActorId, ActorRole, result.Message);
                return BadRequest(new
                {
                    message = result.Message,
                    errors = result.Errors
                });
            }

            logger.LogInformation("AddService: Service {ServiceId} ('{ServiceName}') created by {ActorName} (ID: {ActorId}) (Role: {ActorRole})",
                result.Data.Id, result.Data.ServiceName, ActorName, ActorId, ActorRole);
            return CreatedAtAction(nameof(GetServiceById), new { id = result.Data.Id }, result.Data);
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<ServiceDto>> UpdateService(int id, UpdateServiceDto service)
        {
            var result = await Service.UpdateServiceAsync(id, service);

            if (!result.IsSuccess)
            {
                logger.LogWarning("UpdateService: Failed for service {ServiceId} by {ActorName} (ID: {ActorId}) (Role: {ActorRole}). Reason: {Message}",
                    id, ActorName, ActorId, ActorRole, result.Message);
                return BadRequest(new
                {
                    message = result.Message,
                    errors = result.Errors
                });
            }

            logger.LogInformation("UpdateService: Service {ServiceId} updated by {ActorName} (ID: {ActorId}) (Role: {ActorRole})",
                id, ActorName, ActorId, ActorRole);
            return Ok(result.Data);
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult> DeleteService(int id)
        {
            var result = await Service.DeleteServiceAsync(id);

            if (!result)
            {
                logger.LogDebug("DeleteService: Service {ServiceId} not found, requested by {ActorName} (ID: {ActorId}) (Role: {ActorRole})",
                    id, ActorName, ActorId, ActorRole);
                return NotFound($"Service with ID {id} was not found");
            }

            logger.LogInformation("DeleteService: Service {ServiceId} deleted by {ActorName} (ID: {ActorId}) (Role: {ActorRole})",
                id, ActorName, ActorId, ActorRole);
            return Ok($"Service with ID {id} successfully deleted.");
        }
    }

}
