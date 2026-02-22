using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ServiceBookingPlatform.Data;
using ServiceBookingPlatform.Models;
using ServiceBookingPlatform.Models.Dtos.User;
using ServiceBookingPlatform.Services;

namespace UnitTests;

public class UserRegistrationServiceTests
{
    private static readonly PasswordHasher<User> _passwordHasher = new();

    private AppDbContext GetInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }

    [Fact]
    public async Task RegisterUserAsync_ValidUser_ReturnsSuccess()
    {
        // Arrange
        var context = GetInMemoryDbContext();
        var userRegistrationService = new UserRegistrationService(context);

        var userDto = new UserDto
        {
            FirstName = "John",
            LastName = "Doe",
            Email = "john.doe@example.com",
            Password = "Pass1!",
            PhoneNumber = "07123456789",
            Role = "Customer"
        };

        // Act
        var result = await userRegistrationService.RegisterUserAsync(userDto);

        // Assert
        Assert.True(result.Success);
        Assert.Equal("User registered successfully", result.Message);

        // Verify user was added to database
        var user = await context.Users.FirstOrDefaultAsync(u => u.Email == "john.doe@example.com");
        Assert.NotNull(user);
        Assert.Equal("John", user.FirstName);
        Assert.Equal("Doe", user.LastName);
        Assert.Equal("Customer", user.Role);
    }

    [Fact]
    public async Task RegisterUserAsync_DuplicateEmail_ReturnsFailure()
    {
        // Arrange
        var context = GetInMemoryDbContext();
        var userRegistrationService = new UserRegistrationService(context);

        // Add existing user
        var existingUser = new User
        {
            FirstName = "Existing",
            LastName = "User",
            Email = "existing@example.com",
            PasswordHash = "hashedpassword",
            PhoneNumber = "07123456789",
            Role = "Customer"
        };
        context.Users.Add(existingUser);
        await context.SaveChangesAsync();

        var userDto = new UserDto
        {
            FirstName = "New",
            LastName = "User",
            Email = "existing@example.com",
            Password = "Pass1!",
            PhoneNumber = "07987654321",
            Role = "Customer"
        };

        // Act
        var result = await userRegistrationService.RegisterUserAsync(userDto);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("Email address is already registered", result.Message);
    }

    [Fact]
    public async Task RegisterUserAsync_InvalidFirstName_ReturnsFailure()
    {
        // Arrange
        var context = GetInMemoryDbContext();
        var userRegistrationService = new UserRegistrationService(context);

        var userDto = new UserDto
        {
            FirstName = "", // Invalid: empty first name
            LastName = "Doe",
            Email = "john.doe@example.com",
            Password = "Pass1!",
            PhoneNumber = "07123456789",
            Role = "Customer"
        };

        // Act
        var result = await userRegistrationService.RegisterUserAsync(userDto);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("First name is required", result.Message);
    }

    [Fact]
    public async Task RegisterUserAsync_FirstNameTooShort_ReturnsFailure()
    {
        // Arrange
        var context = GetInMemoryDbContext();
        var userRegistrationService = new UserRegistrationService(context);

        var userDto = new UserDto
        {
            FirstName = "J", // Invalid: too short
            LastName = "Doe",
            Email = "john.doe@example.com",
            Password = "Pass1!",
            PhoneNumber = "07123456789",
            Role = "Customer"
        };

        // Act
        var result = await userRegistrationService.RegisterUserAsync(userDto);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("First name must be between 2 and 50 characters", result.Message);
    }

    [Fact]
    public async Task RegisterUserAsync_InvalidLastName_ReturnsFailure()
    {
        // Arrange
        var context = GetInMemoryDbContext();
        var userRegistrationService = new UserRegistrationService(context);

        var userDto = new UserDto
        {
            FirstName = "John",
            LastName = "", // Invalid: empty last name
            Email = "john.doe@example.com",
            Password = "Pass1!",
            PhoneNumber = "07123456789",
            Role = "Customer"
        };

        // Act
        var result = await userRegistrationService.RegisterUserAsync(userDto);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("Last name is required", result.Message);
    }

    [Fact]
    public async Task RegisterUserAsync_InvalidEmail_ReturnsFailure()
    {
        // Arrange
        var context = GetInMemoryDbContext();
        var userRegistrationService = new UserRegistrationService(context);

        var userDto = new UserDto
        {
            FirstName = "John",
            LastName = "Doe",
            Email = "invalid-email", // Invalid email format
            Password = "Pass1!",
            PhoneNumber = "07123456789",
            Role = "Customer"
        };

        // Act
        var result = await userRegistrationService.RegisterUserAsync(userDto);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("Email address is not in a valid format", result.Message);
    }

    [Fact]
    public async Task RegisterUserAsync_EmptyEmail_ReturnsFailure()
    {
        // Arrange
        var context = GetInMemoryDbContext();
        var userRegistrationService = new UserRegistrationService(context);

        var userDto = new UserDto
        {
            FirstName = "John",
            LastName = "Doe",
            Email = "",
            Password = "Pass1!",
            PhoneNumber = "07123456789",
            Role = "Customer"
        };

        // Act
        var result = await userRegistrationService.RegisterUserAsync(userDto);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("Email is required", result.Message);
    }

    [Fact]
    public async Task RegisterUserAsync_WeakPassword_ReturnsFailure()
    {
        // Arrange
        var context = GetInMemoryDbContext();
        var userRegistrationService = new UserRegistrationService(context);

        var userDto = new UserDto
        {
            FirstName = "John",
            LastName = "Doe",
            Email = "john.doe@example.com",
            Password = "weak", // Weak password
            PhoneNumber = "07123456789",
            Role = "Customer"
        };

        // Act
        var result = await userRegistrationService.RegisterUserAsync(userDto);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("Password must be 6-10 characters", result.Message);
    }

    [Fact]
    public async Task RegisterUserAsync_InvalidPhoneNumber_ReturnsFailure()
    {
        // Arrange
        var context = GetInMemoryDbContext();
        var userRegistrationService = new UserRegistrationService(context);

        var userDto = new UserDto
        {
            FirstName = "John",
            LastName = "Doe",
            Email = "john.doe@example.com",
            Password = "Pass1!",
            PhoneNumber = "123", // Invalid phone number
            Role = "Customer"
        };

        // Act
        var result = await userRegistrationService.RegisterUserAsync(userDto);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("Phone number is not in a valid UK format", result.Message);
    }

    [Fact]
    public async Task RegisterUserAsync_EmptyPhoneNumber_ReturnsFailure()
    {
        // Arrange
        var context = GetInMemoryDbContext();
        var userRegistrationService = new UserRegistrationService(context);

        var userDto = new UserDto
        {
            FirstName = "John",
            LastName = "Doe",
            Email = "john.doe@example.com",
            Password = "Pass1!",
            PhoneNumber = "",
            Role = "Customer"
        };

        // Act
        var result = await userRegistrationService.RegisterUserAsync(userDto);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("Phone number is required", result.Message);
    }

    [Fact]
    public async Task RegisterUserAsync_InvalidRole_ReturnsFailure()
    {
        // Arrange
        var context = GetInMemoryDbContext();
        var userRegistrationService = new UserRegistrationService(context);

        var userDto = new UserDto
        {
            FirstName = "John",
            LastName = "Doe",
            Email = "john.doe@example.com",
            Password = "Pass1!",
            PhoneNumber = "07123456789",
            Role = "InvalidRole"
        };

        // Act
        var result = await userRegistrationService.RegisterUserAsync(userDto);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("Role must be Customer, Staff, or Admin", result.Message);
    }

    [Fact]
    public async Task RegisterUserAsync_DefaultsToCustomerRole_WhenRoleNotProvided()
    {
        // Arrange
        var context = GetInMemoryDbContext();
        var userRegistrationService = new UserRegistrationService(context);

        var userDto = new UserDto
        {
            FirstName = "John",
            LastName = "Doe",
            Email = "john.doe@example.com",
            Password = "Pass1!",
            PhoneNumber = "07123456789",
            Role = "" // Empty role should default to Customer
        };

        // Act
        var result = await userRegistrationService.RegisterUserAsync(userDto);

        // Assert
        Assert.True(result.Success);

        var user = await context.Users.FirstOrDefaultAsync(u => u.Email == "john.doe@example.com");
        Assert.NotNull(user);
        Assert.Equal("Customer", user.Role);
    }

    [Fact]
    public async Task RegisterUserAsync_ValidStaffRole_ReturnsSuccess()
    {
        // Arrange
        var context = GetInMemoryDbContext();
        var userRegistrationService = new UserRegistrationService(context);

        var userDto = new UserDto
        {
            FirstName = "Jane",
            LastName = "Smith",
            Email = "jane.smith@example.com",
            Password = "Pass1!",
            PhoneNumber = "07123456789",
            Role = "Staff"
        };

        // Act
        var result = await userRegistrationService.RegisterUserAsync(userDto);

        // Assert
        Assert.True(result.Success);

        var user = await context.Users.FirstOrDefaultAsync(u => u.Email == "jane.smith@example.com");
        Assert.NotNull(user);
        Assert.Equal("Staff", user.Role);
    }

    [Fact]
    public async Task RegisterUserAsync_ValidAdminRole_ReturnsSuccess()
    {
        // Arrange
        var context = GetInMemoryDbContext();
        var userRegistrationService = new UserRegistrationService(context);

        var userDto = new UserDto
        {
            FirstName = "Admin",
            LastName = "User",
            Email = "admin@example.com",
            Password = "Pass1!",
            PhoneNumber = "07123456789",
            Role = "Admin"
        };

        // Act
        var result = await userRegistrationService.RegisterUserAsync(userDto);

        // Assert
        Assert.True(result.Success);

        var user = await context.Users.FirstOrDefaultAsync(u => u.Email == "admin@example.com");
        Assert.NotNull(user);
        Assert.Equal("Admin", user.Role);
    }

    [Fact]
    public async Task EmailExistsAsync_ExistingEmail_ReturnsTrue()
    {
        // Arrange
        var context = GetInMemoryDbContext();
        var userRegistrationService = new UserRegistrationService(context);

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
        await context.SaveChangesAsync();

        // Act
        var exists = await userRegistrationService.EmailExistsAsync("test@example.com");

        // Assert
        Assert.True(exists);
    }

    [Fact]
    public async Task EmailExistsAsync_NonExistingEmail_ReturnsFalse()
    {
        // Arrange
        var context = GetInMemoryDbContext();
        var userRegistrationService = new UserRegistrationService(context);

        // Act
        var exists = await userRegistrationService.EmailExistsAsync("nonexistent@example.com");

        // Assert
        Assert.False(exists);
    }

    [Fact]
    public async Task ValidateUserDto_ValidUser_ReturnsValid()
    {
        // Arrange
        var context = GetInMemoryDbContext();
        var userRegistrationService = new UserRegistrationService(context);

        var userDto = new UserDto
        {
            FirstName = "John",
            LastName = "Doe",
            Email = "john.doe@example.com",
            Password = "Pass1!",
            PhoneNumber = "07123456789",
            Role = "Customer"
        };

        // Act
        var result = userRegistrationService.ValidateUserDto(userDto);

        // Assert
        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public async Task ValidateUserDto_MultipleErrors_ReturnsAllErrors()
    {
        // Arrange
        var context = GetInMemoryDbContext();
        var userRegistrationService = new UserRegistrationService(context);

        var userDto = new UserDto
        {
            FirstName = "", // Invalid
            LastName = "D", // Too short
            Email = "invalid-email", // Invalid format
            Password = "weak", // Weak password
            PhoneNumber = "", // Empty
            Role = "Customer"
        };

        // Act
        var result = userRegistrationService.ValidateUserDto(userDto);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains("First name is required", result.Errors);
        Assert.Contains("Last name must be between 2 and 50 characters", result.Errors);
        Assert.Contains("Email address is not in a valid format", result.Errors);
        Assert.Contains("Password must be 6-10 characters and contain at least one uppercase letter, one lowercase letter, one digit, and one special character", result.Errors);
        Assert.Contains("Phone number is required", result.Errors);
    }
}
