using Microsoft.EntityFrameworkCore;
using ServiceBookingPlatform.Data;
using ServiceBookingPlatform.Models;
using ServiceBookingPlatform.Models.Dtos.Service;
using ServiceBookingPlatform.Services;

namespace UnitTests;

public class UserServiceServiceTests
{
    private AppDbContext GetInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }

    [Fact]
    public async Task CreateServiceAsync_ValidService_ReturnsSuccessResult()
    {
        // Arrange
        var context = GetInMemoryDbContext();
        var service = new UserServiceService(context);
        var createDto = new CreateServiceDto
        {
            ServiceName = "House Cleaning",
            ServiceType = "Cleaning",
            ServiceDescription = "Professional house cleaning service"
        };

        // Act
        var result = await service.CreateServiceAsync(createDto);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
        Assert.Equal("House Cleaning", result.Data.ServiceName);
        Assert.Equal("Service created successfully.", result.Message);
    }

    [Fact]
    public async Task CreateServiceAsync_DuplicateServiceName_ReturnsFailure()
    {
        // Arrange
        var context = GetInMemoryDbContext();
        var service = new UserServiceService(context);
        
        // Add an existing service
        context.Services.Add(new Service
        {
            ServiceName = "Plumbing",
            ServiceType = "Repair",
            ServiceDescription = "Plumbing services"
        });

        await context.SaveChangesAsync();

        var createDto = new CreateServiceDto
        {
            ServiceName = "Plumbing",
            ServiceType = "Repair",
            ServiceDescription = "Another plumbing service"
        };

        // Act
        var result = await service.CreateServiceAsync(createDto);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("A service with the same name already exists.", result.Message);
    }

    [Fact]
    public async Task GetServiceByIdAsync_ExistingService_ReturnsService()
    {
        // Arrange
        var context = GetInMemoryDbContext();
        var service = new UserServiceService(context);
        
        var existingService = new Service
        {
            ServiceName = "Electrical Repair",
            ServiceType = "Repair",
            ServiceDescription = "Electrical repair services"
        };
        context.Services.Add(existingService);
        await context.SaveChangesAsync();

        // Act
        var result = await service.GetServiceByIdAsync(existingService.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Electrical Repair", result.ServiceName);
        Assert.Equal("Repair", result.ServiceType);
    }

    [Fact]
    public async Task GetServiceByIdAsync_NonExistingService_ReturnsNull()
    {
        // Arrange
        var context = GetInMemoryDbContext();
        var service = new UserServiceService(context);

        // Act
        var result = await service.GetServiceByIdAsync(999);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetAllServicesAsync_ReturnsAllServices()
    {
        // Arrange
        var context = GetInMemoryDbContext();
        var service = new UserServiceService(context);
        
        context.Services.AddRange(
            new Service { ServiceName = "Service 1", ServiceType = "Type A", ServiceDescription = "Description 1" },
            new Service { ServiceName = "Service 2", ServiceType = "Type B", ServiceDescription = "Description 2" },
            new Service { ServiceName = "Service 3", ServiceType = "Type C", ServiceDescription = "Description 3" }
        );
        await context.SaveChangesAsync();

        // Act
        var result = await service.GetAllServicesAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(3, result.Count);
    }

    [Fact]
    public async Task DeleteServiceAsync_ExistingService_ReturnsTrue()
    {
        // Arrange
        var context = GetInMemoryDbContext();
        var service = new UserServiceService(context);
        
        var existingService = new Service
        {
            ServiceName = "Gardening",
            ServiceType = "Outdoor",
            ServiceDescription = "Gardening services"
        };
        context.Services.Add(existingService);
        await context.SaveChangesAsync();

        // Act
        var result = await service.DeleteServiceAsync(existingService.Id);

        // Assert
        Assert.True(result);
        Assert.Empty(context.Services);
    }

    [Fact]
    public async Task DeleteServiceAsync_NonExistingService_ReturnsFalse()
    {
        // Arrange
        var context = GetInMemoryDbContext();
        var service = new UserServiceService(context);

        // Act
        var result = await service.DeleteServiceAsync(999);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task ServiceExistsAsync_ExistingService_ReturnsTrue()
    {
        // Arrange
        var context = GetInMemoryDbContext();
        var service = new UserServiceService(context);
        
        var existingService = new Service
        {
            ServiceName = "Painting",
            ServiceType = "Renovation",
            ServiceDescription = "Professional painting service"
        };
        context.Services.Add(existingService);
        await context.SaveChangesAsync();

        // Act
        var result = await service.ServiceExistsAsync(existingService.Id);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task ServiceExistsAsync_NonExistingService_ReturnsFalse()
    {
        // Arrange
        var context = GetInMemoryDbContext();
        var service = new UserServiceService(context);

        // Act
        var result = await service.ServiceExistsAsync(999);

        // Assert
        Assert.False(result);
    }
}
