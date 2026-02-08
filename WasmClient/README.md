# Service Booking Platform - Blazor WebAssembly Client

A Blazor WebAssembly standalone application for managing service bookings.

## ?? Features

- **JWT Authentication** with automatic token refresh
- **Role-based Authorization** (Customer, Staff, Admin)
- **Service Browsing** - View all available services
- **Booking Management** - Create, view, update, and delete bookings
- **Responsive UI** with Bootstrap 5
- **Automatic API Error Handling**

## ??? Architecture

### Project Structure
```
WasmClient/
??? Api/                        # HTTP API clients
?   ??? ApiClientBase.cs        # Base class for all API clients
?   ??? UserLogInApiClient.cs   # Login endpoint
?   ??? RefreshApiClient.cs     # Token refresh endpoint
?   ??? UserBookingApiClient.cs # Booking CRUD operations
?   ??? UserServiceApiClient.cs # Service operations
??? Components/                 # Reusable components
?   ??? RedirectToLogin.razor   # Unauthorized redirect
??? Handlers/                   # HTTP message handlers
?   ??? AuthorizationMessageHandler.cs  # Token attachment & refresh
??? Layout/                     # Layout components
?   ??? MainLayout.razor        # Navigation & layout
??? Models/                     # DTOs and domain models
?   ??? Dtos/                   # Data transfer objects
?   ??? Configuration/          # Configuration models
??? Pages/                      # Routable pages
?   ??? Home.razor             # Landing page
?   ??? Login.razor            # Authentication
?   ??? Bookings.razor         # Booking list
?   ??? Services.razor         # Service catalog
??? Services/                   # Business logic services
?   ??? TokenService.cs        # JWT token management (localStorage)
?   ??? CustomAuthStateProvider.cs  # Authentication state
??? wwwroot/                    # Static assets
?   ??? appsettings.json       # Configuration
?   ??? appsettings.Development.json
??? App.razor                   # Root component
??? Program.cs                  # Service registration & startup
??? _Imports.razor             # Global using directives
```

## ?? Authentication Flow

### 1. Login
```csharp
User enters credentials ? UserLogInApiClient.LoginAsync() 
? Receive JWT tokens ? Store in localStorage 
? Notify AuthenticationStateProvider ? Navigate to home
```

### 2. API Requests with Token Refresh
```csharp
Request initiated ? TokenRefreshHandler intercepts 
? Attach access token ? API responds with 401 (if expired)
? Call RefreshApiClient.RefreshAsync() 
? Update tokens in localStorage ? Retry original request
```

### 3. Logout
```csharp
User clicks logout ? Clear tokens from localStorage 
? Notify AuthenticationStateProvider ? Redirect to login
```

## ?? API Integration

### Configuration
API base URL is configured in `wwwroot/appsettings.json`:

```json
{
  "ApiBaseUrl": "https://localhost:7022"
}
```

### HttpClient Architecture
Each API client is registered with dependency injection:

```csharp
builder.Services.AddHttpClient<IUserBookingApiClient, UserBookingApiClient>(client =>
{
    client.BaseAddress = new Uri(apiBaseUrl);
})
.AddHttpMessageHandler<TokenRefreshHandler>();  // Auto-attaches JWT tokens
```

**Important:** `RefreshApiClient` does NOT use the handler to avoid circular refresh loops.

### Making API Calls
Example from a Razor component:

```csharp
@inject IUserBookingApiClient BookingClient

protected override async Task OnInitializedAsync()
{
    var bookings = await BookingClient.GetAllBookingsAsync();
    // Tokens are automatically attached by TokenRefreshHandler
}
```

## ?? Token Management

Tokens are stored in browser localStorage via JavaScript interop:

```csharp
public class TokenService : ITokenService
{
    public async Task SetTokensAsync(string accessToken, string refreshToken)
    {
        await jsRuntime.InvokeVoidAsync("localStorage.setItem", "accessToken", accessToken);
        await jsRuntime.InvokeVoidAsync("localStorage.setItem", "refreshToken", refreshToken);
    }
}
```

### Security Considerations
- ?? **Development:** localStorage is acceptable for rapid prototyping
- ? **Production:** Migrate to HTTP-only cookies to prevent XSS attacks

## ?? Running the Application

### Prerequisites
- .NET 10 SDK
- Backend API running on `https://localhost:7022`

### Development
```bash
# Navigate to WasmClient directory
cd WasmClient

# Run the application
dotnet run

# Or use Visual Studio launch profile (F5)
```

The app will launch on:
- HTTPS: `https://localhost:7234`
- HTTP: `http://localhost:5261`

### Building for Production
```bash
dotnet publish -c Release -o ./publish
```

Output will be in `./publish/wwwroot` - deploy to any static file host:
- Azure Static Web Apps
- GitHub Pages
- Netlify
- AWS S3 + CloudFront

## ?? Configuration

### Launch Settings
Edit `..\WasmClient\Properties\launchSettings.json`:

```json
{
  "profiles": {
    "https": {
      "applicationUrl": "https://localhost:7234;http://localhost:5261",
      "ApiBaseUrl": "https://localhost:7022"
    }
  }
}
```

### CORS
The backend API must allow your Blazor app's origin. In API's `Program.cs`:

```csharp
builder.Services.AddCors(options =>
{
    options.AddPolicy("BlazorWasmPolicy", policy =>
    {
        policy.WithOrigins(
            "https://localhost:7234",  // Must match Blazor app URL
            "http://localhost:5261"
        )
        .AllowAnyMethod()
        .AllowAnyHeader()
        .AllowCredentials();
    });
});

// In middleware pipeline:
app.UseCors("BlazorWasmPolicy");
```

## ?? Key Components

### Login Page (`Pages/Login.razor`)
- Email/password form with validation
- Error handling for failed login attempts
- Automatic redirect after successful authentication

### Bookings Page (`Pages/Bookings.razor`)
- Displays user's bookings (role-based filtering handled by API)
- CRUD operations (Create, Read, Update, Delete)
- Status badges with color coding

### Services Page (`Pages/Services.razor`)
- Browse available services
- View pricing and duration
- Quick "Book Now" action

### Main Layout (`Layout/MainLayout.razor`)
- Bootstrap navigation bar
- Conditional rendering based on authentication state
- User info display and logout button

## ??? Extending the Application

### Adding a New API Client

1. **Create the client:**
```csharp
public interface IMyApiClient
{
    Task<MyDto> GetDataAsync();
}

public class MyApiClient : ApiClientBase, IMyApiClient
{
    public MyApiClient(HttpClient httpClient, ILogger<MyApiClient> logger)
        : base(httpClient, logger) { }

    public async Task<MyDto> GetDataAsync()
        => await GetAsync<MyDto>("api/myendpoint");
}
```

2. **Register in `Program.cs`:**
```csharp
builder.Services.AddHttpClient<IMyApiClient, MyApiClient>(client =>
{
    client.BaseAddress = new Uri(apiBaseUrl);
})
.AddHttpMessageHandler<TokenRefreshHandler>();
```

3. **Use in components:**
```razor
@inject IMyApiClient MyClient

@code {
    protected override async Task OnInitializedAsync()
    {
        var data = await MyClient.GetDataAsync();
    }
}
```

## ?? Troubleshooting

### CORS Errors
**Error:** "Access to fetch has been blocked by CORS policy"

**Fix:** Ensure API's CORS policy includes your Blazor app URL:
```csharp
policy.WithOrigins("https://localhost:7234")
```

### 401 Unauthorized
**Error:** API returns 401 even with valid credentials

**Fix:** 
1. Check JWT configuration in API's `appsettings.json`
2. Verify token is being attached in browser DevTools ? Network tab
3. Ensure API's JWT validation parameters match token issuer/audience

### Token Not Refreshing
**Error:** User gets logged out after 15 minutes

**Fix:**
1. Check `TokenRefreshHandler` is registered
2. Verify `RefreshApiClient` does NOT use the handler
3. Inspect browser console for errors during refresh attempt

### LocalStorage Not Persisting
**Error:** User gets logged out on page refresh

**Fix:**
1. Ensure JavaScript interop is working (check browser console)
2. Verify browser allows localStorage (not in incognito mode with strict settings)

## ?? Best Practices Implemented

? **Separation of Concerns** - API clients, services, and UI are decoupled

? **DRY Principle** - `ApiClientBase` eliminates code duplication

? **Error Handling** - Try-catch blocks with user-friendly messages

? **Logging** - ILogger injected into all API clients

? **Dependency Injection** - All services registered in `Program.cs`

? **Authorization** - `[Authorize]` attribute on protected pages

? **Token Security** - Automatic refresh prevents session expiration

## ?? Future Enhancements

- [ ] **SignalR Integration** - Real-time booking updates
- [ ] **Offline Support** - Service worker for PWA functionality
- [ ] **HTTP-only Cookies** - More secure token storage
- [ ] **Rate Limiting** - Client-side throttling for API calls
- [ ] **Validation** - FluentValidation for complex forms
- [ ] **State Management** - Fluxor for predictable state updates
- [ ] **Unit Tests** - bUnit for component testing
- [ ] **E2E Tests** - Playwright for full user flow testing

## ?? Support

For issues or questions:
1. Check API logs in backend console
2. Inspect browser DevTools ? Console & Network tabs
3. Review authentication state in Application ? Local Storage

---

**Built with** ?? **using Blazor WebAssembly & .NET 10**
