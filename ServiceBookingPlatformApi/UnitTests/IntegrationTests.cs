using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ServiceBookingPlatform;
using ServiceBookingPlatform.Data;
using ServiceBookingPlatform.Models;
using ServiceBookingPlatform.Models.Dtos.Booking;
using ServiceBookingPlatform.Models.Dtos.Service;
using ServiceBookingPlatform.Models.Dtos.User;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace UnitTests;

/// <summary>
/// integration tests using WebApplicationFactory to test real end-to-end scenarios.
/// 
/// **DbContext Configuration Solution:**
/// The dual-provider issue has been resolved by conditionally registering the database in Program.cs.
/// When environment is "Testing", SQL Server is not registered, allowing tests to use InMemory database.
/// 
/// Test Coverage:
/// - Login → Refresh → Protected Endpoint (full auth flow)
/// - Token rotation and security
/// - Booking ownership enforcement (customers can only see their own bookings)
/// - Role-based authorization (Staff/Admin can see all bookings)
/// - Service CRUD operations end-to-end
/// - Registration validation
/// - Booking conflict detection
/// - Complete customer workflow from registration to booking cancellation
/// 
/// 
/// Total: 15 integration tests covering critical user flows and security boundaries
/// </summary>
public class IntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;
    private static readonly string DatabaseName = $"TestDb_{Guid.NewGuid()}";
    private static int _bookingCounter = 0;
    private static int _userCounter = 0;

    public IntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                // Use a single shared in-memory database for all requests in this test
                // This ensures data persists across HTTP requests
                services.AddDbContext<AppDbContext>(options =>
                {
                    options.UseInMemoryDatabase(DatabaseName);
                });
            });
        })
        .WithWebHostBuilder(builder =>
        {
            // Set environment to "Testing" so Program.cs doesn't register SQL Server
            builder.UseSetting("Environment", "Testing");
        });

        _client = _factory.CreateClient();
    }

    #region Helper Methods

    private async Task<UserLogInResponseDto?> RegisterAndLoginUser(string email, string password, string role = "Customer")
    {
        // Make email unique by appending a counter
        var counter = Interlocked.Increment(ref _userCounter);
        var uniqueEmail = email.Replace("@", $"{counter}@");
        
        // Register user
        var registerDto = new UserDto
        {
            FirstName = "Test",
            LastName = "User",
            Email = uniqueEmail,
            Password = password,
            PhoneNumber = "07700900123",
            Role = role
        };

        var registerResponse = await _client.PostAsJsonAsync("/api/UserRegistration", registerDto);
        var registerContent = await registerResponse.Content.ReadAsStringAsync();
        
        if (!registerResponse.IsSuccessStatusCode)
        {
            throw new Exception($"Registration failed: {registerResponse.StatusCode} - {registerContent}");
        }

        // Login user
        var loginDto = new UserLogInRequestDto
        {
            Email = uniqueEmail,
            Password = password
        };

        var loginResponse = await _client.PostAsJsonAsync("/api/UserLogIn", loginDto);
        var loginContent = await loginResponse.Content.ReadAsStringAsync();
        
        if (!loginResponse.IsSuccessStatusCode)
        {
            throw new Exception($"Login failed: {loginResponse.StatusCode} - {loginContent}");
        }
        
        return await loginResponse.Content.ReadFromJsonAsync<UserLogInResponseDto>();
    }

    private async Task<int> CreateTestService(string accessToken, string serviceName = "")
    {
        // Generate unique service name if not provided
        if (string.IsNullOrEmpty(serviceName))
        {
            serviceName = $"TestService_{Guid.NewGuid().ToString()[..8]}";
        }
        
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var serviceDto = new CreateServiceDto
        {
            ServiceName = serviceName,
            ServiceType = "Testing",
            ServiceDescription = "Test service for integration tests"
        };

        var response = await _client.PostAsJsonAsync("/api/UserService", serviceDto);
        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            throw new Exception($"Service creation failed: {response.StatusCode} - {errorContent}");
        }
        
        // Service creation returns ServiceDto via CreatedAtAction
        var service = await response.Content.ReadFromJsonAsync<ServiceDto>();
        return service!.Id;
    }

    private async Task<int> CreateTestBooking(string accessToken, int serviceId)
    {
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        // Use incrementing counter to avoid booking conflicts
        var counter = Interlocked.Increment(ref _bookingCounter);
        var bookingDto = new CreateBookingDto
        {
            ServiceId = serviceId,
            ScheduledStart = DateTime.UtcNow.AddDays(counter),
            ScheduledEnd = DateTime.UtcNow.AddDays(counter).AddHours(1),
            Status = "Pending"
        };

        var response = await _client.PostAsJsonAsync("/api/UserBooking", bookingDto);
        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            throw new Exception($"Booking creation failed: {response.StatusCode} - {errorContent}");
        }
        
        // Booking creation returns BookingDto via CreatedAtAction
        var booking = await response.Content.ReadFromJsonAsync<BookingDto>();
        return booking!.Id;
    }

    #endregion

    #region Authentication Flow Tests

    [Fact]
    public async Task Login_RefreshToken_ProtectedEndpoint_CompleteFlow()
    {
        // 1. Register and Login
        var userDto = await RegisterAndLoginUser("flow@test.com", "Test123!", "Admin");
        Assert.NotNull(userDto);
        Assert.NotNull(userDto.AccessToken);
        Assert.NotNull(userDto.RefreshToken);

        // 2. Create a service so we have data to retrieve
        var serviceId = await CreateTestService(userDto.AccessToken);
        
        // 3. Access protected endpoint with access token
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", userDto.AccessToken);
        var servicesResponse = await _client.GetAsync("/api/UserService");
        Assert.Equal(HttpStatusCode.OK, servicesResponse.StatusCode);

        // 4. Wait a moment (simulating time passing)
        await Task.Delay(100);

        // 5. Use refresh token to get new access token
        var refreshRequest = new RefreshTokenRequestDto(userDto.RefreshToken);

        var refreshResponse = await _client.PostAsJsonAsync("/auth/refresh", refreshRequest);
        Assert.Equal(HttpStatusCode.OK, refreshResponse.StatusCode);

        var newTokens = await refreshResponse.Content.ReadFromJsonAsync<UserLogInResponseDto>();
        Assert.NotNull(newTokens);
        Assert.NotNull(newTokens.AccessToken);
        Assert.NotNull(newTokens.RefreshToken);
        // Refresh token should always be different (token rotation)
        Assert.NotEqual(userDto.RefreshToken, newTokens.RefreshToken);
        // Access token might be the same if generated within the same second with same user data,
        // but the important security feature is that refresh token rotates

        // 6. Use new access token to access protected endpoint
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", newTokens.AccessToken);
        var servicesResponse2 = await _client.GetAsync("/api/UserService");
        Assert.Equal(HttpStatusCode.OK, servicesResponse2.StatusCode);

        // 7. Try to use old refresh token (should fail due to rotation)
        var oldRefreshRequest = new RefreshTokenRequestDto(userDto.RefreshToken);

        var oldRefreshResponse = await _client.PostAsJsonAsync("/auth/refresh", oldRefreshRequest);
        Assert.Equal(HttpStatusCode.Unauthorized, oldRefreshResponse.StatusCode);
    }

    [Fact]
    public async Task Login_Logout_RefreshFails()
    {
        // 1. Register and Login
        var userDto = await RegisterAndLoginUser("logout@test.com", "Test123!");
        Assert.NotNull(userDto);

        // 2. Logout
        var logoutRequest = new RefreshTokenRequestDto(userDto.RefreshToken);

        var logoutResponse = await _client.PostAsJsonAsync("/api/UserLogIn/logout", logoutRequest);
        Assert.Equal(HttpStatusCode.OK, logoutResponse.StatusCode);

        // 3. Try to use refresh token after logout (should fail)
        var refreshResponse = await _client.PostAsJsonAsync("/auth/refresh", logoutRequest);
        Assert.Equal(HttpStatusCode.Unauthorized, refreshResponse.StatusCode);
    }

    [Fact]
    public async Task AccessProtectedEndpoint_WithoutToken_Returns401()
    {
        // Try to access protected endpoint without authentication
        var response = await _client.GetAsync("/api/UserBooking");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task AccessProtectedEndpoint_WithInvalidToken_Returns401()
    {
        // Try to access protected endpoint with invalid token
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "invalid-token");
        var response = await _client.GetAsync("/api/UserBooking");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    #endregion

    #region Booking Ownership Tests

    [Fact]
    public async Task Customer_CanOnlySeeOwnBookings()
    {
        // Create admin for service creation and two customers for bookings
        var admin = await RegisterAndLoginUser("admin@test.com", "Test123!", "Admin");
        var customer1 = await RegisterAndLoginUser("customer1@test.com", "Test123!", "Customer");
        var customer2 = await RegisterAndLoginUser("customer2@test.com", "Test123!", "Customer");
        Assert.NotNull(admin);
        Assert.NotNull(customer1);
        Assert.NotNull(customer2);

        // Admin creates a service
        var serviceId = await CreateTestService(admin.AccessToken);

        // Customer 1 creates a booking
        var booking1Id = await CreateTestBooking(customer1.AccessToken, serviceId);
        Assert.True(booking1Id > 0);

        // Customer 2 creates a booking
        var booking2Id = await CreateTestBooking(customer2.AccessToken, serviceId);
        Assert.True(booking2Id > 0);

        // Customer 1 gets all bookings (should only see their own)
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", customer1.AccessToken);
        var customer1Response = await _client.GetAsync("/api/UserBooking");
        var customer1Bookings = await customer1Response.Content.ReadFromJsonAsync<List<BookingDto>>();
        Assert.NotNull(customer1Bookings);
        Assert.Single(customer1Bookings); // Customer should only see 1 booking
        Assert.Equal(booking1Id, customer1Bookings[0].Id);

        // Customer 2 gets all bookings (should only see their own)
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", customer2.AccessToken);
        var customer2Response = await _client.GetAsync("/api/UserBooking");
        var customer2Bookings = await customer2Response.Content.ReadFromJsonAsync<List<BookingDto>>();
        Assert.NotNull(customer2Bookings);
        Assert.Single(customer2Bookings); // Customer should only see 1 booking
        Assert.Equal(booking2Id, customer2Bookings[0].Id);
    }

    [Fact]
    public async Task Customer_CannotAccessOtherCustomerBooking()
    {
        // Create admin for service and two customers
        var admin = await RegisterAndLoginUser("admin3@test.com", "Test123!", "Admin");
        var customer1 = await RegisterAndLoginUser("owner@test.com", "Test123!", "Customer");
        var customer2 = await RegisterAndLoginUser("other@test.com", "Test123!", "Customer");
        Assert.NotNull(admin);
        Assert.NotNull(customer1);
        Assert.NotNull(customer2);

        // Admin creates service, customer1 creates booking
        var serviceId = await CreateTestService(admin.AccessToken);
        var bookingId = await CreateTestBooking(customer1.AccessToken, serviceId);

        // Customer2 tries to access customer1's booking
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", customer2.AccessToken);
        var response = await _client.GetAsync($"/api/UserBooking/{bookingId}");
        
        // Should return 403 Forbidden or 404 Not Found (depending on implementation)
        Assert.True(response.StatusCode == HttpStatusCode.Forbidden || 
                   response.StatusCode == HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Staff_CanSeeAllBookings()
    {
        // Create admin for service, customer and staff
        var admin = await RegisterAndLoginUser("admin2@test.com", "Test123!", "Admin");
        var customer = await RegisterAndLoginUser("customer@test.com", "Test123!", "Customer");
        var staff = await RegisterAndLoginUser("staff@test.com", "Test123!", "Staff");
        Assert.NotNull(admin);
        Assert.NotNull(customer);
        Assert.NotNull(staff);

        // Admin creates service, customer creates booking
        var serviceId = await CreateTestService(admin.AccessToken);
        var bookingId = await CreateTestBooking(customer.AccessToken, serviceId);

        // Staff gets all bookings (should see customer's booking)
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", staff.AccessToken);
        var staffResponse = await _client.GetAsync("/api/UserBooking");
        var staffBookings = await staffResponse.Content.ReadFromJsonAsync<List<BookingDto>>();
        Assert.NotNull(staffBookings);
        Assert.NotEmpty(staffBookings);
        Assert.Contains(staffBookings, b => b.Id == bookingId);
    }

    [Fact]
    public async Task Admin_CanSeeAllBookings()
    {
        // Create one admin and one customer
        var admin = await RegisterAndLoginUser("admin@test.com", "Test123!", "Admin");
        var customer = await RegisterAndLoginUser("customer2@test.com", "Test123!", "Customer");
        Assert.NotNull(customer);
        Assert.NotNull(admin);

        // Admin creates service, customer creates booking
        var serviceId = await CreateTestService(admin.AccessToken);
        var bookingId = await CreateTestBooking(customer.AccessToken, serviceId);

        // Admin gets all bookings (should see customer's booking)
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", admin.AccessToken);
        var adminResponse = await _client.GetAsync("/api/UserBooking");
        var adminBookings = await adminResponse.Content.ReadFromJsonAsync<List<BookingDto>>();
        Assert.NotNull(adminBookings);
        Assert.NotEmpty(adminBookings);
        Assert.Contains(adminBookings, b => b.Id == bookingId);
    }

    #endregion

    #region JWT Token Validation Tests

    [Fact]
    public async Task JwtToken_ContainsCorrectClaims()
    {
        // Register and login
        var userDto = await RegisterAndLoginUser("claims@test.com", "Test123!", "Admin");
        Assert.NotNull(userDto);

        // Decode the JWT token
        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(userDto.AccessToken);

        // Verify claims exist and have correct types
        Assert.Contains(jwtToken.Claims, c => c.Type == JwtRegisteredClaimNames.NameId);
        Assert.Contains(jwtToken.Claims, c => c.Type == JwtRegisteredClaimNames.Email && !string.IsNullOrEmpty(c.Value));
        Assert.Contains(jwtToken.Claims, c => c.Type == "role" && c.Value == "Admin");
    }

    #endregion

    #region Booking Conflict Tests

    [Fact]
    public async Task CreateBooking_WithConflictingTime_ShouldFail()
    {
        // Register admin and customer
        var admin = await RegisterAndLoginUser("admin4@test.com", "Test123!", "Admin");
        var customer = await RegisterAndLoginUser("conflict@test.com", "Test123!", "Customer");
        Assert.NotNull(admin);
        Assert.NotNull(customer);

        // Admin creates a service
        var serviceId = await CreateTestService(admin.AccessToken);

        // Create first booking
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", customer.AccessToken);
        
        var startTime = DateTime.UtcNow.AddDays(2);
        var endTime = startTime.AddHours(2);

        var booking1 = new CreateBookingDto
        {
            ServiceId = serviceId,
            ScheduledStart = startTime,
            ScheduledEnd = endTime,
            Status = "Pending"
        };

        var response1 = await _client.PostAsJsonAsync("/api/UserBooking", booking1);
        Assert.Equal(HttpStatusCode.Created, response1.StatusCode);

        // Try to create overlapping booking (should fail)
        var booking2 = new CreateBookingDto
        {
            ServiceId = serviceId,
            ScheduledStart = startTime.AddMinutes(30), // Overlaps with first booking
            ScheduledEnd = endTime.AddMinutes(30),
            Status = "Pending"
        };

        var response2 = await _client.PostAsJsonAsync("/api/UserBooking", booking2);
        Assert.Equal(HttpStatusCode.BadRequest, response2.StatusCode);
    }

    #endregion

    #region Service Management Tests

    [Fact]
    public async Task CreateService_GetService_UpdateService_DeleteService_FullCycle()
    {
        // Register and login as Admin
        var user = await RegisterAndLoginUser("service@test.com", "Test123!", "Admin");
        Assert.NotNull(user);

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", user.AccessToken);

        // 1. Create Service
        var createDto = new CreateServiceDto
        {
            ServiceName = "Full Cycle Service",
            ServiceType = "Testing",
            ServiceDescription = "Initial description"
        };

        var createResponse = await _client.PostAsJsonAsync("/api/UserService", createDto);
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var service = await createResponse.Content.ReadFromJsonAsync<ServiceDto>();
        Assert.NotNull(service);
        var serviceId = service.Id;

        // 2. Get Service by ID
        var getResponse = await _client.GetAsync($"/api/UserService/{serviceId}");
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
        var retrievedService = await getResponse.Content.ReadFromJsonAsync<ServiceDto>();
        Assert.NotNull(retrievedService);
        Assert.Equal("Full Cycle Service", retrievedService.ServiceName);

        // 3. Update Service
        var updateDto = new UpdateServiceDto
        {
            ServiceDescription = "Updated description"
        };

        var updateResponse = await _client.PatchAsJsonAsync($"/api/UserService/{serviceId}", updateDto);
        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);

        // Verify update
        var getUpdatedResponse = await _client.GetAsync($"/api/UserService/{serviceId}");
        var updatedService = await getUpdatedResponse.Content.ReadFromJsonAsync<ServiceDto>();
        Assert.NotNull(updatedService);
        Assert.Equal("Updated description", updatedService.ServiceDescription);

        // 4. Delete Service
        var deleteResponse = await _client.DeleteAsync($"/api/UserService/{serviceId}");
        Assert.Equal(HttpStatusCode.OK, deleteResponse.StatusCode);

        // Verify deletion
        var getDeletedResponse = await _client.GetAsync($"/api/UserService/{serviceId}");
        Assert.Equal(HttpStatusCode.NotFound, getDeletedResponse.StatusCode);
    }

    #endregion

    #region Registration Validation Tests

    [Fact]
    public async Task Register_WithDuplicateEmail_ShouldFail()
    {
        // Register first user
        var user1 = new UserDto
        {
            FirstName = "First",
            LastName = "User",
            Email = "duplicate@test.com",
            Password = "Test123!",
            PhoneNumber = "07700900123",
            Role = "Customer"
        };

        var response1 = await _client.PostAsJsonAsync("/api/UserRegistration", user1);
        Assert.Equal(HttpStatusCode.OK, response1.StatusCode);

        // Try to register with same email
        var user2 = new UserDto
        {
            FirstName = "Second",
            LastName = "User",
            Email = "duplicate@test.com", // Same email
            Password = "Test456!",
            PhoneNumber = "07700900456",
            Role = "Customer"
        };

        var response2 = await _client.PostAsJsonAsync("/api/UserRegistration", user2);
        Assert.Equal(HttpStatusCode.BadRequest, response2.StatusCode);
    }

    [Fact]
    public async Task Register_WithInvalidPassword_ShouldFail()
    {
        var user = new UserDto
        {
            FirstName = "Test",
            LastName = "User",
            Email = "weakpass@test.com",
            Password = "weak", // Weak password
            PhoneNumber = "07700900123",
            Role = "Customer"
        };

        var response = await _client.PostAsJsonAsync("/api/UserRegistration", user);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    #endregion

    #region End-to-End Scenarios

    [Fact]
    public async Task CompleteBookingWorkflow_CustomerPerspective()
    {
        // 1. Customer registers and logs in
        var customer = await RegisterAndLoginUser("workflow@test.com", "Test123!", "Customer");
        var admin = await RegisterAndLoginUser("workflowadmin@test.com", "Test123!", "Admin");
        Assert.NotNull(customer);
        Assert.NotNull(admin);

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", customer.AccessToken);

        // 2. Admin creates a service (in real world, this would be done by admin/staff)
        var serviceId = await CreateTestService(admin.AccessToken);

        // 3. View available services
        var servicesResponse = await _client.GetAsync("/api/UserService");
        var services = await servicesResponse.Content.ReadFromJsonAsync<List<ServiceDto>>();
        Assert.NotNull(services);
        Assert.NotEmpty(services);
        Assert.Contains(services, s => s.Id == serviceId);

        // 4. Create a booking
        var bookingDto = new CreateBookingDto
        {
            ServiceId = serviceId,
            ScheduledStart = DateTime.UtcNow.AddDays(3),
            ScheduledEnd = DateTime.UtcNow.AddDays(3).AddHours(1),
            Status = "Pending"
        };

        var createBookingResponse = await _client.PostAsJsonAsync("/api/UserBooking", bookingDto);
        Assert.Equal(HttpStatusCode.Created, createBookingResponse.StatusCode);
        var booking = await createBookingResponse.Content.ReadFromJsonAsync<BookingDto>();
        Assert.NotNull(booking);
        var bookingId = booking.Id;

        // 5. View their bookings
        var bookingsResponse = await _client.GetAsync("/api/UserBooking");
        var bookings = await bookingsResponse.Content.ReadFromJsonAsync<List<BookingDto>>();
        Assert.NotNull(bookings);
        Assert.Contains(bookings, b => b.Id == bookingId);

        // 6. Update booking status
        var updateDto = new UpdateBookingDto
        {
            ScheduledStart = DateTime.UtcNow.AddDays(4),
            ScheduledEnd = DateTime.UtcNow.AddDays(4).AddHours(1),
            Status = "Confirmed"
        };

        var updateResponse = await _client.PatchAsJsonAsync($"/api/UserBooking/{bookingId}", updateDto);
        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);

        // 7. Cancel booking (delete)
        var deleteResponse = await _client.DeleteAsync($"/api/UserBooking/{bookingId}");
        Assert.Equal(HttpStatusCode.OK, deleteResponse.StatusCode);

        // 8. Verify deletion
        var verifyResponse = await _client.GetAsync("/api/UserBooking");
        var remainingBookings = await verifyResponse.Content.ReadFromJsonAsync<List<BookingDto>>();
        Assert.NotNull(remainingBookings);
        Assert.DoesNotContain(remainingBookings, b => b.Id == bookingId);

        // 9. Logout
        var logoutRequest = new RefreshTokenRequestDto(customer.RefreshToken);

        var logoutResponse = await _client.PostAsJsonAsync("/api/UserLogIn/logout", logoutRequest);
        Assert.Equal(HttpStatusCode.OK, logoutResponse.StatusCode);
    }

    [Fact]
    public async Task MultipleRefreshes_TokenRotation_Works()
    {
        // Register and login
        var user = await RegisterAndLoginUser("multirefresh@test.com", "Test123!");
        Assert.NotNull(user);

        var currentRefreshToken = user.RefreshToken;
        var usedRefreshTokens = new List<string> { currentRefreshToken };

        // Perform multiple refreshes
        for (int i = 0; i < 3; i++)
        {
            var refreshRequest = new RefreshTokenRequestDto(currentRefreshToken);

            var refreshResponse = await _client.PostAsJsonAsync("/auth/refresh", refreshRequest);
            Assert.Equal(HttpStatusCode.OK, refreshResponse.StatusCode);

            var newTokens = await refreshResponse.Content.ReadFromJsonAsync<UserLogInResponseDto>();
            Assert.NotNull(newTokens);
            Assert.NotNull(newTokens.RefreshToken);

            // Verify new token is different from all previous tokens
            Assert.DoesNotContain(newTokens.RefreshToken, usedRefreshTokens);

            currentRefreshToken = newTokens.RefreshToken;
            usedRefreshTokens.Add(currentRefreshToken);

            // Verify we can use the new access token
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", newTokens.AccessToken);
            var servicesResponse = await _client.GetAsync("/api/UserService");
            Assert.Equal(HttpStatusCode.OK, servicesResponse.StatusCode);
        }

        // Verify old tokens don't work
        foreach (var oldToken in usedRefreshTokens.Take(usedRefreshTokens.Count - 1))
        {
            var oldRefreshRequest = new RefreshTokenRequestDto(oldToken);

            var oldRefreshResponse = await _client.PostAsJsonAsync("/auth/refresh", oldRefreshRequest);
            Assert.Equal(HttpStatusCode.Unauthorized, oldRefreshResponse.StatusCode);
        }
    }

    #endregion
}
