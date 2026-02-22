using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ServiceBookingPlatform.Data;
using ServiceBookingPlatform.Services;
using ServiceBookingPlatform.Controllers;
using ServiceBookingPlatform.Models;
using ServiceBookingPlatform.Models.Dtos.Booking;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;

namespace UnitTests;

public class UserBookingServiceTests
{
    private AppDbContext GetInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }

    private async Task<(User user, Service service)> SeedTestData(AppDbContext context)
    {
        var user = new User
        {
            FirstName = "Test",
            LastName = "User",
            Email = "test@example.com",
            PasswordHash = "hashedpassword",
            PhoneNumber = "07123456789",
            Role = "Customer"
        };
        context.Users.Add(user);

        var service = new Service
        {
            ServiceName = "Test Service",
            ServiceType = "Cleaning",
            ServiceDescription = "Test cleaning service"
        };
        context.Services.Add(service);

        await context.SaveChangesAsync();
        return (user, service);
    }

    private ClaimsPrincipal CreateClaimsPrincipal(int userId, string email, string role)
    {
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(ClaimTypes.Email, email),
            new Claim(ClaimTypes.Role, role)
        };
        return new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));
    }

    [Fact]
    public async Task GetAllBookings_Customer_OnlySeesOwnBookings()
    {
        // Arrange
        var context = GetInMemoryDbContext();
        var userBookingService = new UserBookingService(context);

        // Create two customers
        var customer1Id = 1;
        var customer2Id = 2;

        // Create test users
        var customer1 = new User
        {
            Id = customer1Id,
            FirstName = "Customer1",
            LastName = "Test",
            Email = "customer1@test.com",
            PasswordHash = "hash",
            PhoneNumber = "1234567890",
            Role = "Customer"
        };

        var customer2 = new User
        {
            Id = customer2Id,
            FirstName = "Customer2",
            LastName = "Test",
            Email = "customer2@test.com",
            PasswordHash = "hash",
            PhoneNumber = "0987654321",
            Role = "Customer"
        };

        context.Users.AddRange(customer1, customer2);

        // Create a service
        var service = new Service
        {
            Id = 1,
            ServiceName = "Test Service",
            ServiceType = "Testing",
            ServiceDescription = "Test"
        };
        context.Services.Add(service);
        await context.SaveChangesAsync();

        // Create bookings for both customers
        var booking1 = new Booking
        {
            UserId = customer1Id,
            ServiceId = service.Id,
            ScheduledStart = DateTime.UtcNow.AddDays(1),
            ScheduledEnd = DateTime.UtcNow.AddDays(1).AddHours(1),
            Status = "Pending"
        };

        var booking2 = new Booking
        {
            UserId = customer2Id,
            ServiceId = service.Id,
            ScheduledStart = DateTime.UtcNow.AddDays(2),
            ScheduledEnd = DateTime.UtcNow.AddDays(2).AddHours(1),
            Status = "Pending"
        };

        context.Bookings.AddRange(booking1, booking2);
        await context.SaveChangesAsync();

        // Create ClaimsPrincipal for customer1
        var claimsPrincipal = CreateClaimsPrincipal(customer1Id, customer1.Email, "Customer");

        // Act
        var bookings = await userBookingService.GetAllBookingsAsync(claimsPrincipal);

        // Assert
        Assert.Single(bookings); // Customer should only see their own booking
        Assert.All(bookings, b => Assert.Equal(customer1.Email, b.Email)); // All bookings should belong to customer1
    }

    [Fact]
    public async Task GetAllBookingsAsync_WithBookings_ReturnsAllBookings()
    {
        // Arrange
        var context = GetInMemoryDbContext();
        var userBookingService = new UserBookingService(context);
        var (user, service) = await SeedTestData(context);

        var booking1 = new Booking
        {
            UserId = user.Id,
            ServiceId = service.Id,
            ScheduledStart = DateTime.UtcNow.AddDays(1),
            ScheduledEnd = DateTime.UtcNow.AddDays(1).AddHours(2),
            Status = "Pending"
        };

        var booking2 = new Booking
        {
            UserId = user.Id,
            ServiceId = service.Id,
            ScheduledStart = DateTime.UtcNow.AddDays(2),
            ScheduledEnd = DateTime.UtcNow.AddDays(2).AddHours(3),
            Status = "Confirmed"
        };

        context.Bookings.AddRange(booking1, booking2);
        await context.SaveChangesAsync();

        var claimsPrincipal = CreateClaimsPrincipal(user.Id, user.Email, "Admin");

        // Act
        var bookings = await userBookingService.GetAllBookingsAsync(claimsPrincipal);

        // Assert
        Assert.Equal(2, bookings.Count);
    }

    [Fact]
    public async Task GetAllBookingsAsync_NoBookings_ReturnsEmptyList()
    {
        // Arrange
        var context = GetInMemoryDbContext();
        var userBookingService = new UserBookingService(context);
        var (user, _) = await SeedTestData(context);

        var claimsPrincipal = CreateClaimsPrincipal(user.Id, user.Email, "Admin");

        // Act
        var bookings = await userBookingService.GetAllBookingsAsync(claimsPrincipal);

        // Assert
        Assert.Empty(bookings);
    }

    [Fact]
    public async Task GetBookingByIdAsync_ExistingBooking_ReturnsBooking()
    {
        // Arrange
        var context = GetInMemoryDbContext();
        var userBookingService = new UserBookingService(context);
        var (user, service) = await SeedTestData(context);

        var booking = new Booking
        {
            UserId = user.Id,
            ServiceId = service.Id,
            ScheduledStart = DateTime.UtcNow.AddDays(1),
            ScheduledEnd = DateTime.UtcNow.AddDays(1).AddHours(2),
            Status = "Pending"
        };
        context.Bookings.Add(booking);
        await context.SaveChangesAsync();

        var claimsPrincipal = CreateClaimsPrincipal(user.Id, user.Email, "Customer");

        // Act
        var result = await userBookingService.GetBookingByIdAsync(booking.Id, claimsPrincipal);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(booking.Id, result.Id);
        Assert.Equal("User", result.LastName);
        Assert.Equal("Test Service", result.ServiceName);
    }

    [Fact]
    public async Task GetBookingByIdAsync_NonExistingBooking_ThrowsException()
    {
        // Arrange
        var context = GetInMemoryDbContext();
        var userBookingService = new UserBookingService(context);
        var (user, _) = await SeedTestData(context);

        var claimsPrincipal = CreateClaimsPrincipal(user.Id, user.Email, "Customer");

        // Act & Assert
        await Assert.ThrowsAsync<NullReferenceException>(
            async () => await userBookingService.GetBookingByIdAsync(999, claimsPrincipal));
    }

    [Fact]
    public async Task CreateBookingAsync_ValidBooking_ReturnsSuccess()
    {
        // Arrange
        var context = GetInMemoryDbContext();
        var userBookingService = new UserBookingService(context);
        var (user, service) = await SeedTestData(context);

        var createDto = new CreateBookingDto
        {
            ServiceId = service.Id,
            ScheduledStart = DateTime.UtcNow.AddDays(1),
            ScheduledEnd = DateTime.UtcNow.AddDays(1).AddHours(2),
            Status = "Pending"
        };

        // Act
        var result = await userBookingService.CreateBookingAsync(user.Id, createDto);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
        Assert.Equal("Booking created successfully.", result.Message);
    }

    [Fact]
    public async Task CreateBookingAsync_EndBeforeStart_ReturnsFailure()
    {
        // Arrange
        var context = GetInMemoryDbContext();
        var userBookingService = new UserBookingService(context);
        var (user, service) = await SeedTestData(context);

        var createDto = new CreateBookingDto
        {
            ServiceId = service.Id,
            ScheduledStart = DateTime.UtcNow.AddDays(1),
            ScheduledEnd = DateTime.UtcNow.AddDays(1).AddHours(-1), // End before start
            Status = "Pending"
        };

        // Act
        var result = await userBookingService.CreateBookingAsync(user.Id, createDto);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("Scheduled end time must be after scheduled start time.", result.Message);
    }

    [Fact]
    public async Task CreateBookingAsync_StartInPast_ReturnsFailure()
    {
        // Arrange
        var context = GetInMemoryDbContext();
        var userBookingService = new UserBookingService(context);
        var (user, service) = await SeedTestData(context);

        var createDto = new CreateBookingDto
        {
            ServiceId = service.Id,
            ScheduledStart = DateTime.UtcNow.AddDays(-1), // In the past
            ScheduledEnd = DateTime.UtcNow.AddDays(-1).AddHours(2),
            Status = "Pending"
        };

        // Act
        var result = await userBookingService.CreateBookingAsync(user.Id, createDto);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("Scheduled start time must be in the future.", result.Message);
    }

    [Fact]
    public async Task CreateBookingAsync_DurationExceeds8Hours_ReturnsFailure()
    {
        // Arrange
        var context = GetInMemoryDbContext();
        var userBookingService = new UserBookingService(context);
        var (user, service) = await SeedTestData(context);

        var createDto = new CreateBookingDto
        {
            ServiceId = service.Id,
            ScheduledStart = DateTime.UtcNow.AddDays(1),
            ScheduledEnd = DateTime.UtcNow.AddDays(1).AddHours(9), // 9 hours duration
            Status = "Pending"
        };

        // Act
        var result = await userBookingService.CreateBookingAsync(user.Id, createDto);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("Booking duration cannot exceed 8 hours.", result.Message);
    }

    [Fact]
    public async Task CreateBookingAsync_InvalidStatus_ReturnsFailure()
    {
        // Arrange
        var context = GetInMemoryDbContext();
        var userBookingService = new UserBookingService(context);
        var (user, service) = await SeedTestData(context);

        var createDto = new CreateBookingDto
        {
            ServiceId = service.Id,
            ScheduledStart = DateTime.UtcNow.AddDays(1),
            ScheduledEnd = DateTime.UtcNow.AddDays(1).AddHours(2),
            Status = "InvalidStatus"
        };

        // Act
        var result = await userBookingService.CreateBookingAsync(user.Id, createDto);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("Invalid booking status", result.Message);
    }

    [Fact]
    public async Task CreateBookingAsync_NonExistentUser_ReturnsFailure()
    {
        // Arrange
        var context = GetInMemoryDbContext();
        var userBookingService = new UserBookingService(context);
        var (_, service) = await SeedTestData(context);

        var createDto = new CreateBookingDto
        {
            ServiceId = service.Id,
            ScheduledStart = DateTime.UtcNow.AddDays(1),
            ScheduledEnd = DateTime.UtcNow.AddDays(1).AddHours(2),
            Status = "Pending"
        };

        // Act
        var result = await userBookingService.CreateBookingAsync(999, createDto); // Non-existent user ID

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("The specified user does not exist.", result.Message);
    }

    [Fact]
    public async Task CreateBookingAsync_NonExistentService_ReturnsFailure()
    {
        // Arrange
        var context = GetInMemoryDbContext();
        var userBookingService = new UserBookingService(context);
        var (user, _) = await SeedTestData(context);

        var createDto = new CreateBookingDto
        {
            ServiceId = 999, // Non-existent service
            ScheduledStart = DateTime.UtcNow.AddDays(1),
            ScheduledEnd = DateTime.UtcNow.AddDays(1).AddHours(2),
            Status = "Pending"
        };

        // Act
        var result = await userBookingService.CreateBookingAsync(user.Id, createDto);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("The specified service does not exist.", result.Message);
    }

    [Fact]
    public async Task CreateBookingAsync_ConflictingBooking_ReturnsFailure()
    {
        // Arrange
        var context = GetInMemoryDbContext();
        var userBookingService = new UserBookingService(context);
        var (user, service) = await SeedTestData(context);

        // Create an existing booking
        var existingBooking = new Booking
        {
            UserId = user.Id,
            ServiceId = service.Id,
            ScheduledStart = DateTime.UtcNow.AddDays(1),
            ScheduledEnd = DateTime.UtcNow.AddDays(1).AddHours(4),
            Status = "Confirmed"
        };
        context.Bookings.Add(existingBooking);
        await context.SaveChangesAsync();

        // Try to create a conflicting booking
        var createDto = new CreateBookingDto
        {
            ServiceId = service.Id,
            ScheduledStart = DateTime.UtcNow.AddDays(1).AddHours(2), // Overlaps with existing
            ScheduledEnd = DateTime.UtcNow.AddDays(1).AddHours(6),
            Status = "Pending"
        };

        // Act
        var result = await userBookingService.CreateBookingAsync(user.Id, createDto);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("The service is already booked for the requested time.", result.Message);
    }

    [Fact]
    public async Task UpdateBookingAsync_ValidUpdate_ReturnsSuccess()
    {
        // Arrange
        var context = GetInMemoryDbContext();
        var userBookingService = new UserBookingService(context);
        var (user, service) = await SeedTestData(context);

        var booking = new Booking
        {
            UserId = user.Id,
            ServiceId = service.Id,
            ScheduledStart = DateTime.UtcNow.AddDays(1),
            ScheduledEnd = DateTime.UtcNow.AddDays(1).AddHours(2),
            Status = "Pending"
        };
        context.Bookings.Add(booking);
        await context.SaveChangesAsync();

        var updateDto = new UpdateBookingDto
        {
            ScheduledStart = DateTime.UtcNow.AddDays(2),
            ScheduledEnd = DateTime.UtcNow.AddDays(2).AddHours(3),
            Status = "Confirmed"
        };

        var claimsPrincipal = CreateClaimsPrincipal(user.Id, user.Email, "Admin");

        // Act
        var result = await userBookingService.UpdateBookingAsync(booking.Id, updateDto, claimsPrincipal);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
        Assert.Equal("Confirmed", result.Data.Status);
    }

    [Fact]
    public async Task UpdateBookingAsync_NonExistentBooking_ReturnsFailure()
    {
        // Arrange
        var context = GetInMemoryDbContext();
        var userBookingService = new UserBookingService(context);
        var (user, _) = await SeedTestData(context);

        var updateDto = new UpdateBookingDto
        {
            ScheduledStart = DateTime.UtcNow.AddDays(2),
            ScheduledEnd = DateTime.UtcNow.AddDays(2).AddHours(3),
            Status = "Confirmed"
        };

        var claimsPrincipal = CreateClaimsPrincipal(user.Id, user.Email, "Admin");

        // Act
        var result = await userBookingService.UpdateBookingAsync(999, updateDto, claimsPrincipal);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("Booking not found.", result.Message);
    }

    [Fact]
    public async Task UpdateBookingAsync_InvalidTimeRange_ReturnsFailure()
    {
        // Arrange
        var context = GetInMemoryDbContext();
        var userBookingService = new UserBookingService(context);
        var (user, service) = await SeedTestData(context);

        var booking = new Booking
        {
            UserId = user.Id,
            ServiceId = service.Id,
            ScheduledStart = DateTime.UtcNow.AddDays(1),
            ScheduledEnd = DateTime.UtcNow.AddDays(1).AddHours(2),
            Status = "Pending"
        };
        context.Bookings.Add(booking);
        await context.SaveChangesAsync();

        var updateDto = new UpdateBookingDto
        {
            ScheduledStart = DateTime.UtcNow.AddDays(2),
            ScheduledEnd = DateTime.UtcNow.AddDays(2).AddHours(-1), // End before start
            Status = "Confirmed"
        };

        var claimsPrincipal = CreateClaimsPrincipal(user.Id, user.Email, "Admin");

        // Act
        var result = await userBookingService.UpdateBookingAsync(booking.Id, updateDto, claimsPrincipal);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("Scheduled end time must be after scheduled start time.", result.Message);
    }

    [Fact]
    public async Task DeleteBookingAsync_ExistingBooking_ReturnsTrue()
    {
        // Arrange
        var context = GetInMemoryDbContext();
        var userBookingService = new UserBookingService(context);
        var (user, service) = await SeedTestData(context);

        var booking = new Booking
        {
            UserId = user.Id,
            ServiceId = service.Id,
            ScheduledStart = DateTime.UtcNow.AddDays(1),
            ScheduledEnd = DateTime.UtcNow.AddDays(1).AddHours(2),
            Status = "Pending"
        };
        context.Bookings.Add(booking);
        await context.SaveChangesAsync();

        var claimsPrincipal = CreateClaimsPrincipal(user.Id, user.Email, "Admin");

        // Act
        var result = await userBookingService.DeleteBookingAsync(booking.Id, claimsPrincipal);

        // Assert
        Assert.True(result);
        var deletedBooking = await context.Bookings.FindAsync(booking.Id);
        Assert.Null(deletedBooking);
    }

    [Fact]
    public async Task DeleteBookingAsync_NonExistentBooking_ReturnsFalse()
    {
        // Arrange
        var context = GetInMemoryDbContext();
        var userBookingService = new UserBookingService(context);
        var (user, _) = await SeedTestData(context);

        var claimsPrincipal = CreateClaimsPrincipal(user.Id, user.Email, "Admin");

        // Act
        var result = await userBookingService.DeleteBookingAsync(999, claimsPrincipal);

        // Assert
        Assert.False(result);
    }

}


