using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Moq;
using ServiceBookingPlatform.Data;
using ServiceBookingPlatform.Models;
using ServiceBookingPlatform.Models.Dtos.User;
using ServiceBookingPlatform.Services;
using ServiceBookingPlatform.Services.Common;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace UnitTests;

// I have used the Moq library to create mock implementations of IUserLogInService for testing purposes.
// This is because of the complex dependencies and behaviors associated with user authentication that are not the primary focus of JwtService tests.

public class JwtServiceTests
{
    private static readonly PasswordHasher<User> _passwordHasher = new();

    private AppDbContext GetInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }

    private IConfiguration GetMockConfiguration()
    {
        var inMemorySettings = new Dictionary<string, string>
        {
            {"JwtConfig:Issuer", "TestIssuer"},
            {"JwtConfig:Audience", "TestAudience"},
            {"JwtConfig:Key", "TestSecretKeyWithAtLeast32Characters"},
            {"JwtConfig:TokenValidityMins", "60"}
        };

        return new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings!)
            .Build();
    }

    private async Task<User> SeedTestUser(AppDbContext context, string email = "test@example.com", string role = "Customer")
    {
        var user = new User
        {
            FirstName = "Test",
            LastName = "User",
            Email = email,
            PasswordHash = _passwordHasher.HashPassword(null!, "Pass1!"),
            PhoneNumber = "07123456789",
            Role = role
        };
        context.Users.Add(user);
        await context.SaveChangesAsync();
        return user;
    }

    [Fact]
    public async Task Authenticate_ValidCredentials_ReturnsToken()
    {
        // Arrange
        var context = GetInMemoryDbContext();
        var config = GetMockConfiguration();
        
        // Seed a test user in the database
        var testUser = await SeedTestUser(context, "test@example.com", "Customer");

        var mockUserLogInService = new Mock<IUserLogInService>();
        mockUserLogInService
            .Setup(s => s.ValidateUserCredentialsAsync(It.IsAny<UserLogInRequestDto>()))
            .ReturnsAsync((true, "Log in Successful"));

        var jwtService = new JwtService(context, config, mockUserLogInService.Object);

        var loginRequest = new UserLogInRequestDto
        {
            Email = "test@example.com",
            Password = "Pass1!"
        };

        // Act
        var result = await jwtService.Authenticate(loginRequest);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("test@example.com", result.Email);
        Assert.NotNull(result.AccessToken);
        Assert.True(result.ExpiresIn > 0);
    }

    [Fact]
    public async Task Authenticate_InvalidCredentials_ReturnsNull()
    {
        // Arrange
        var context = GetInMemoryDbContext();
        var config = GetMockConfiguration();

        var mockUserLogInService = new Mock<IUserLogInService>();
        mockUserLogInService
            .Setup(s => s.ValidateUserCredentialsAsync(It.IsAny<UserLogInRequestDto>()))
            .ReturnsAsync((false, "Password is incorrect"));

        var jwtService = new JwtService(context, config, mockUserLogInService.Object);

        var loginRequest = new UserLogInRequestDto
        {
            Email = "test@example.com",
            Password = "WrongPass1!"
        };

        // Act
        var result = await jwtService.Authenticate(loginRequest);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task Authenticate_ValidToken_ContainsCorrectClaims()
    {
        // Arrange
        var context = GetInMemoryDbContext();
        var config = GetMockConfiguration();
        
        // Seed a test user in the database
        var testUser = await SeedTestUser(context, "test@example.com", "Admin");

        var mockUserLogInService = new Mock<IUserLogInService>();
        mockUserLogInService
            .Setup(s => s.ValidateUserCredentialsAsync(It.IsAny<UserLogInRequestDto>()))
            .ReturnsAsync((true, "Log in Successful"));

        var jwtService = new JwtService(context, config, mockUserLogInService.Object);

        var loginRequest = new UserLogInRequestDto
        {
            Email = "test@example.com",
            Password = "Pass1!"
        };

        // Act
        var result = await jwtService.Authenticate(loginRequest);

        // Assert
        Assert.NotNull(result);

        var tokenHandler = new JwtSecurityTokenHandler();
        var token = tokenHandler.ReadJwtToken(result.AccessToken);

        // Verify Email claim
        var emailClaim = token.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Email);
        Assert.NotNull(emailClaim);
        Assert.Equal("test@example.com", emailClaim.Value);

        // Verify NameId claim (User ID)
        var nameIdClaim = token.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.NameId);
        Assert.NotNull(nameIdClaim);
        Assert.Equal(testUser.Id.ToString(), nameIdClaim.Value);

        // Verify Role claim (checking multiple possible claim type formats)
        var roleClaim = token.Claims.FirstOrDefault(c => 
            c.Type == ClaimTypes.Role || 
            c.Type == "role" ||
            c.Type.Contains("role", StringComparison.OrdinalIgnoreCase));
        Assert.NotNull(roleClaim);
        Assert.Equal("Admin", roleClaim.Value);
    }

    [Fact]
    public async Task Authenticate_ValidToken_HasCorrectIssuerAndAudience()
    {
        // Arrange
        var context = GetInMemoryDbContext();
        var config = GetMockConfiguration();
        
        // Seed a test user in the database
        await SeedTestUser(context, "test@example.com", "Staff");

        var mockUserLogInService = new Mock<IUserLogInService>();
        mockUserLogInService
            .Setup(s => s.ValidateUserCredentialsAsync(It.IsAny<UserLogInRequestDto>()))
            .ReturnsAsync((true, "Log in Successful"));

        var jwtService = new JwtService(context, config, mockUserLogInService.Object);

        var loginRequest = new UserLogInRequestDto
        {
            Email = "test@example.com",
            Password = "Pass1!"
        };

        // Act
        var result = await jwtService.Authenticate(loginRequest);

        // Assert
        Assert.NotNull(result);

        var tokenHandler = new JwtSecurityTokenHandler();
        var token = tokenHandler.ReadJwtToken(result.AccessToken);

        Assert.Equal("TestIssuer", token.Issuer);
        Assert.Contains("TestAudience", token.Audiences);
    }

    [Fact]
    public async Task Authenticate_ValidToken_ExpiresInApproximately60Minutes()
    {
        // Arrange
        var context = GetInMemoryDbContext();
        var config = GetMockConfiguration();
        
        // Seed a test user in the database
        await SeedTestUser(context, "test@example.com", "Customer");

        var mockUserLogInService = new Mock<IUserLogInService>();
        mockUserLogInService
            .Setup(s => s.ValidateUserCredentialsAsync(It.IsAny<UserLogInRequestDto>()))
            .ReturnsAsync((true, "Log in Successful"));

        var jwtService = new JwtService(context, config, mockUserLogInService.Object);

        var loginRequest = new UserLogInRequestDto
        {
            Email = "test@example.com",
            Password = "Pass1!"
        };

        // Act
        var result = await jwtService.Authenticate(loginRequest);

        // Assert
        Assert.NotNull(result);

        // ExpiresIn should be around 60 mins, a margin of error is acceptable
        Assert.InRange(result.ExpiresIn, 3590, 3600);
    }

    [Fact]
    public async Task Authenticate_UserNotFound_ReturnsNull()
    {
        // Arrange
        var context = GetInMemoryDbContext();
        var config = GetMockConfiguration();

        var mockUserLogInService = new Mock<IUserLogInService>();
        mockUserLogInService
            .Setup(s => s.ValidateUserCredentialsAsync(It.IsAny<UserLogInRequestDto>()))
            .ReturnsAsync((false, "User not found in database"));

        var jwtService = new JwtService(context, config, mockUserLogInService.Object);

        var loginRequest = new UserLogInRequestDto
        {
            Email = "nonexistent@example.com",
            Password = "Pass1!"
        };

        // Act
        var result = await jwtService.Authenticate(loginRequest);

        // Assert
        Assert.Null(result);
    }
     
    /* Implementing Mock:
    Mock<IInterface> mock = new Mock<IInterface>();
    mock.Setup(x => x.Method(parameters)).Returns(result);
    var instance = mock.Object;*/

   
}
