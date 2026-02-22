using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ServiceBookingPlatform.Models.Dtos.Service;
using ServiceBookingPlatform.Services;

namespace ServiceBookingPlatform.Controllers
{
    [Route("api/[controller]")]
    [ApiController]

    public class UserServiceController(IUserServiceService Service) : ControllerBase
    {
        [HttpGet]
        public async Task<ActionResult<List<ServiceDto>>> GetAllServices()
        {
            var services = await Service.GetAllServicesAsync();

            if (services.Count == 0)
            {
                return NotFound("No services found.");
            }

            return Ok(services);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<ServiceDto>> GetServiceById(int id)
        {
            var service = await Service.GetServiceByIdAsync(id);
            return service is null
                ? NotFound($"Service with ID {id} was not found")
                : Ok(service);
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<ServiceDto>> AddService(CreateServiceDto service)
        {
            var result = await Service.CreateServiceAsync(service);

            if (!result.IsSuccess || result.Data is null)
            {
                return BadRequest(new
                {
                    message = result.Message,
                    errors = result.Errors
                });
            }

            return CreatedAtAction(nameof(GetServiceById), new { id = result.Data.Id }, result.Data);
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<ServiceDto>> UpdateService(int id, UpdateServiceDto service)
        {
            var result = await Service.UpdateServiceAsync(id, service);

            if (!result.IsSuccess)
            {
                return BadRequest(new
                {
                    message = result.Message,
                    errors = result.Errors
                });
            }

            return Ok(result.Data);
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult> DeleteService(int id)
        {
            var result = await Service.DeleteServiceAsync(id);
            return result
                ? Ok($"Service with ID {id} successfully deleted.")
                : NotFound($"Service with ID {id} was not found");
        }
    }
}
