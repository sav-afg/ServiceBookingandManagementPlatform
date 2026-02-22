using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using ServiceBookingPlatform.Data;
using ServiceBookingPlatform.Models;
using ServiceBookingPlatform.Models.Dtos.User;
using ServiceBookingPlatform.Services;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace UnitTests;

public class RefreshServiceTests
{
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
            PasswordHash = "hashedPassword",
            PhoneNumber = "07123456789",
            Role = role
        };
        context.Users.Add(user);
        await context.SaveChangesAsync();
        return user;
    }

    private async Task<RefreshToken> SeedRefreshToken(AppDbContext context, int userId, bool isRevoked = false, DateTime? expiresAt = null)
    {
        var refreshToken = new RefreshToken
        {
            UserId = userId,
            Token = TokenService.GenerateRefreshToken(),
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = expiresAt ?? DateTime.UtcNow.AddDays(7),
            IsRevoked = isRevoked
        };
        context.RefreshTokens.Add(refreshToken);
        await context.SaveChangesAsync();
        return refreshToken;
    }

    [Fact]
    public async Task RefreshTokenAsync_ValidToken_ReturnsNewTokens()
    {
        // Arrange
        var context = GetInMemoryDbContext();
        var config = GetMockConfiguration();
        var refreshService = new RefreshService(context, config);

        var user = await SeedTestUser(context);
        var refreshToken = await SeedRefreshToken(context, user.Id);

        // Act
        var result = await refreshService.RefreshTokenAsync(refreshToken.Token);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
        Assert.Equal(user.Email, result.Data.Email);
        Assert.NotNull(result.Data.AccessToken);
        Assert.NotNull(result.Data.RefreshToken);
        Assert.True(result.Data.ExpiresIn > 0);
        
        // Verify old token was revoked
        var oldToken = await context.RefreshTokens.FindAsync(refreshToken.Id);
        Assert.True(oldToken!.IsRevoked);
        
        // Verify new token was created
        var newToken = await context.RefreshTokens
            .FirstOrDefaultAsync(t => t.Token == result.Data.RefreshToken);
        Assert.NotNull(newToken);
        Assert.Equal(user.Id, newToken.UserId);
        Assert.False(newToken.IsRevoked);
    }

    [Fact]
    public async Task RefreshTokenAsync_InvalidToken_ReturnsFailure()
    {
        // Arrange
        var context = GetInMemoryDbContext();
        var config = GetMockConfiguration();
        var refreshService = new RefreshService(context, config);

        var invalidToken = "invalid-token-that-does-not-exist";

        // Act
        var result = await refreshService.RefreshTokenAsync(invalidToken);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("Invalid refresh token", result.Message);
        Assert.Null(result.Data);
    }

    [Fact]
    public async Task RefreshTokenAsync_RevokedToken_ReturnsFailure()
    {
        // Arrange
        var context = GetInMemoryDbContext();
        var config = GetMockConfiguration();
        var refreshService = new RefreshService(context, config);

        var user = await SeedTestUser(context);
        var revokedToken = await SeedRefreshToken(context, user.Id, isRevoked: true);

        // Act
        var result = await refreshService.RefreshTokenAsync(revokedToken.Token);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("Refresh token has been revoked", result.Message);
        Assert.Null(result.Data);
    }

    [Fact]
    public async Task RefreshTokenAsync_ExpiredToken_ReturnsFailure()
    {
        // Arrange
        var context = GetInMemoryDbContext();
        var config = GetMockConfiguration();
        var refreshService = new RefreshService(context, config);

        var user = await SeedTestUser(context);
        var expiredToken = await SeedRefreshToken(
            context, 
            user.Id, 
            isRevoked: false, 
            expiresAt: DateTime.UtcNow.AddDays(-1) // Expired yesterday
        );

        // Act
        var result = await refreshService.RefreshTokenAsync(expiredToken.Token);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("Refresh token has expired", result.Message);
        Assert.Null(result.Data);
    }

    [Fact]
    public async Task RefreshTokenAsync_ValidToken_GeneratesValidJwt()
    {
        // Arrange
        var context = GetInMemoryDbContext();
        var config = GetMockConfiguration();
        var refreshService = new RefreshService(context, config);

        var user = await SeedTestUser(context, "user@test.com", "Admin");
        var refreshToken = await SeedRefreshToken(context, user.Id);

        // Act
        var result = await refreshService.RefreshTokenAsync(refreshToken.Token);

        // Assert
        Assert.True(result.IsSuccess);
        
        // Verify JWT token structure and claims
        var tokenHandler = new JwtSecurityTokenHandler();
        var jwtToken = tokenHandler.ReadJwtToken(result.Data!.AccessToken);
        
        Assert.NotNull(jwtToken);
        Assert.Equal("TestIssuer", jwtToken.Issuer);
        Assert.Contains(jwtToken.Audiences, a => a == "TestAudience");
        
        // Verify claims
        var nameIdClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.NameId);
        var emailClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Email);
        var roleClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == "role");
        
        Assert.NotNull(nameIdClaim);
        Assert.Equal(user.Id.ToString(), nameIdClaim.Value);
        Assert.NotNull(emailClaim);
        Assert.Equal(user.Email, emailClaim.Value);
        Assert.NotNull(roleClaim);
        Assert.Equal(user.Role, roleClaim.Value);
    }

    [Fact]
    public async Task RefreshTokenAsync_ValidToken_TokenRotation()
    {
        // Arrange
        var context = GetInMemoryDbContext();
        var config = GetMockConfiguration();
        var refreshService = new RefreshService(context, config);

        var user = await SeedTestUser(context);
        var originalToken = await SeedRefreshToken(context, user.Id);
        var originalTokenValue = originalToken.Token;

        // Act
        var result = await refreshService.RefreshTokenAsync(originalToken.Token);

        // Assert
        Assert.True(result.IsSuccess);
        
        // Old token should be revoked
        var oldToken = await context.RefreshTokens
            .FirstOrDefaultAsync(t => t.Token == originalTokenValue);
        Assert.NotNull(oldToken);
        Assert.True(oldToken.IsRevoked);
        
        // New token should be different and not revoked
        Assert.NotEqual(originalTokenValue, result.Data!.RefreshToken);
        var newToken = await context.RefreshTokens
            .FirstOrDefaultAsync(t => t.Token == result.Data.RefreshToken);
        Assert.NotNull(newToken);
        Assert.False(newToken.IsRevoked);
        Assert.Equal(user.Id, newToken.UserId);
    }

    [Fact]
    public async Task RefreshTokenAsync_MultipleUsersTokens_IsolatesCorrectly()
    {
        // Arrange
        var context = GetInMemoryDbContext();
        var config = GetMockConfiguration();
        var refreshService = new RefreshService(context, config);

        var user1 = await SeedTestUser(context, "user1@test.com");
        var user2 = await SeedTestUser(context, "user2@test.com");
        
        var token1 = await SeedRefreshToken(context, user1.Id);
        var token2 = await SeedRefreshToken(context, user2.Id);

        // Act
        var result1 = await refreshService.RefreshTokenAsync(token1.Token);
        var result2 = await refreshService.RefreshTokenAsync(token2.Token);

        // Assert
        Assert.True(result1.IsSuccess);
        Assert.True(result2.IsSuccess);
        Assert.Equal("user1@test.com", result1.Data!.Email);
        Assert.Equal("user2@test.com", result2.Data!.Email);
        Assert.NotEqual(result1.Data.RefreshToken, result2.Data.RefreshToken);
    }

    [Fact]
    public async Task RefreshTokenAsync_ValidToken_NewTokenHasCorrectExpiry()
    {
        // Arrange
        var context = GetInMemoryDbContext();
        var config = GetMockConfiguration();
        var refreshService = new RefreshService(context, config);

        var user = await SeedTestUser(context);
        var refreshToken = await SeedRefreshToken(context, user.Id);

        // Act
        var result = await refreshService.RefreshTokenAsync(refreshToken.Token);

        // Assert
        Assert.True(result.IsSuccess);
        
        var newToken = await context.RefreshTokens
            .FirstOrDefaultAsync(t => t.Token == result.Data!.RefreshToken);
        
        Assert.NotNull(newToken);
        Assert.True(newToken.ExpiresAt > DateTime.UtcNow.AddDays(6)); // Should be ~7 days
        Assert.True(newToken.ExpiresAt < DateTime.UtcNow.AddDays(8)); // Should be ~7 days
    }

    [Fact]
    public async Task RefreshTokenAsync_UsingSameTokenTwice_SecondAttemptFails()
    {
        // Arrange
        var context = GetInMemoryDbContext();
        var config = GetMockConfiguration();
        var refreshService = new RefreshService(context, config);

        var user = await SeedTestUser(context);
        var refreshToken = await SeedRefreshToken(context, user.Id);
        var tokenValue = refreshToken.Token;

        // Act
        var firstResult = await refreshService.RefreshTokenAsync(tokenValue);
        var secondResult = await refreshService.RefreshTokenAsync(tokenValue);

        // Assert
        Assert.True(firstResult.IsSuccess);
        Assert.False(secondResult.IsSuccess);
        Assert.Equal("Refresh token has been revoked", secondResult.Message);
    }

    [Fact]
    public async Task GetRefreshToken_ValidToken_ReturnsToken()
    {
        // Arrange
        var context = GetInMemoryDbContext();
        var config = GetMockConfiguration();
        var refreshService = new RefreshService(context, config);

        var user = await SeedTestUser(context);
        var refreshToken = await SeedRefreshToken(context, user.Id);

        var userResponse = new UserLogInResponseDto(
            "some-access-token",
            user.Email,
            3600,
            refreshToken.Token
        );

        // Act
        var result = await refreshService.GetRefreshToken(userResponse);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
        Assert.Equal(refreshToken.Token, result.Data.Token);
        Assert.Equal(user.Id, result.Data.UserId);
    }

    [Fact]
    public async Task GetRefreshToken_InvalidToken_ReturnsFailure()
    {
        // Arrange
        var context = GetInMemoryDbContext();
        var config = GetMockConfiguration();
        var refreshService = new RefreshService(context, config);

        var userResponse = new UserLogInResponseDto(
            "some-access-token",
            "test@example.com",
            3600,
            "non-existent-token"
        );

        // Act
        var result = await refreshService.GetRefreshToken(userResponse);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("Token not found in database", result.Message);
    }
}
