# Service Booking Platform

A modern, full-stack service booking and management platform built with **.NET 10** and **Blazor WebAssembly**. This project demonstrates enterprise-grade architecture, secure authentication, real-time UI updates, and seamless API integration.

## Architecture

This is a **multi-project solution** with clear separation of concerns:

```
ServiceBookingandManagementPlatform/
├── ServiceBookingPlatformApi/    # .NET 10 Web API (Backend)
├── WasmClient/                   # Blazor WebAssembly (Frontend)
└── UnitTests/                    # xUnit Test Suite (92 tests)
```

### Technology Stack

**Backend (API)**
- .NET 10 Web API
- Entity Framework Core with SQL Server
- JWT Authentication with Refresh Tokens
- Serilog for structured logging
- Seq for log aggregation
- xUnit for comprehensive testing (92 passing tests)

**Frontend (Blazor WASM)**
- Blazor WebAssembly (.NET 10)
- Serilog.Sinks.BrowserConsole for client-side logging
- Bootstrap 5 for responsive design
- Bootstrap Icons
- JWT-based authentication with automatic token refresh
- Browser local storage for token persistence

**Integration**
- RESTful API communication via typed HttpClients
- Automatic token refresh with retry logic
- Role-based UI rendering
- CORS-enabled cross-origin requests

---

## Features

### User Management
- User registration with comprehensive validation
- JWT-based authentication (access + refresh tokens)
- Automatic token refresh and rotation
- Secure logout with token revocation
- Password hashing using ASP.NET Core Identity
- Email uniqueness validation
- Role-based authorization (Customer, Staff, Admin)

### Booking Management
- Create, read, update, and delete bookings
- Role-based filtering (Customers see only their bookings)
- Booking status tracking (Pending, Confirmed, Completed, Cancelled)
- Automatic conflict detection (prevents double-booking)
- Real-time UI updates on booking changes

### Service Management
- Service CRUD operations
- Service categorization by type
- Admin-only service creation/modification
- Service browsing for all authenticated users

### Frontend Features
- Responsive design (mobile, tablet, desktop)
- Protected routes with authentication checks
- Automatic navigation on auth state changes
- User-friendly error messages
- Loading states and feedback
- Dark/light mode support via Bootstrap themes

---

## Security & Authentication

### Dual-Token Authentication System

**Access Token (30 minutes)**
- Used for API authorization
- Contains user claims (ID, email, role)
- Sent in `Authorization: Bearer {token}` header
- Automatically refreshed when expired

**Refresh Token (7 days)**
- Stored securely in browser local storage
- Used only to obtain new access tokens
- Rotated on each refresh (security best practice)
- Revoked immediately on logout

### Authentication Flow

```
1. User logs in → API returns access + refresh tokens
2. Frontend stores both tokens in local storage
3. All API calls include access token in Authorization header
4. Access token expires after 30 minutes
5. TokenRefreshHandler intercepts 401 responses
6. Automatically calls /auth/refresh with refresh token
7. Receives new access + refresh tokens
8. Retries original failed request
9. User experience is seamless (no logout required)
```

### Security Features
- Password strength validation (uppercase, lowercase, digit, special char)
- JWT signing with HMAC-SHA256
- Token rotation prevents reuse attacks
- Claims-based authorization in API
- Role-based UI rendering in Blazor
- HTTPS enforcement in production
- SQL injection protection via EF Core
- Rate limiting (100 requests/minute per user)
- CORS with specific origin allowlist
- Global exception handling with safe error messages

---

## Getting Started

### Prerequisites
- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- [SQL Server](https://www.microsoft.com/en-us/sql-server/sql-server-downloads) or SQL Server Express
- [Visual Studio 2026](https://visualstudio.microsoft.com/) (recommended) or VS Code
- [Seq](https://datalust.co/seq) (optional, for log visualization)

### Installation

#### 1. Clone the Repository
```bash
git clone https://github.com/sav-afg/ServiceBookingandManagementPlatform.git
cd ServiceBookingandManagementPlatform
```

#### 2. Configure the API

**Update Database Connection String**

Edit `ServiceBookingPlatformApi/appsettings.json`:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=YOUR_SERVER;Database=ServiceBookingPlatform;Trusted_Connection=True;TrustServerCertificate=True"
  }
}
```

**Update JWT Secret Key (IMPORTANT)**
```json
{
  "JwtConfig": {
    "Key": "YOUR_SECURE_SECRET_KEY_MIN_32_CHARACTERS_CHANGE_THIS_IN_PRODUCTION"
  }
}
```

**Apply Database Migrations**
```bash
cd ServiceBookingPlatformApi
dotnet ef database update
cd ..
```

#### 3. Configure the Frontend

Edit `WasmClient/wwwroot/appsettings.json`:
```json
{
  "ApiBaseUrl": "https://localhost:7022"
}
```

Make sure this matches your API URL.

#### 4. Run the Application

**Option A: Using Visual Studio**
1. Open `ServiceBookingPlatform.slnx`
2. Set multiple startup projects:
   - Right-click solution → Properties → Multiple Startup Projects
   - Set `ServiceBookingPlatformApi` to **Start**
   - Set `WasmClient` to **Start**
3. Press F5 to run both projects

**Option B: Using Command Line**

Terminal 1 (API):
```bash
cd ServiceBookingPlatformApi
dotnet run
```

Terminal 2 (Frontend):
```bash
cd WasmClient
dotnet run
```

The application will be available at:
- **API**: https://localhost:7022
- **API Documentation**: https://localhost:7022/scalar/v1
- **Frontend**: https://localhost:7234 (or check console output)
- **Seq (if running)**: http://localhost:5341

---

## Frontend Architecture

### Project Structure

```
WasmClient/
├── Api/                          # Typed HTTP clients for API calls
│   ├── UserLogInApiClient.cs     # Login/logout API calls
│   ├── UserBookingApiClient.cs   # Booking management
│   ├── UserServiceApiClient.cs   # Service browsing
│   └── RefreshApiClient.cs       # Token refresh
├── Handlers/
│   └── TokenRefreshHandler.cs    # Automatic token refresh on 401
├── Services/
│   ├── CustomAuthStateProvider.cs # JWT parsing & auth state
│   ├── TokenService.cs           # Local storage token management
│   └── JSInteropService.cs       # Browser APIs (localStorage)
├── Pages/
│   ├── Home.razor                # Landing page
│   ├── Login.razor               # Login form
│   ├── Services.razor            # Browse services
│   ├── AddBooking.razor          # Create new booking
│   ├── MyBookings.razor          # View/manage bookings
│   └── AdminDashboard.razor      # Admin-only panel
├── Layout/
│   └── MainLayout.razor          # Navigation, footer, auth display
├── wwwroot/
│   ├── css/app.css               # Custom styles
│   ├── appsettings.json          # Frontend configuration
│   └── index.html                # App entry point
└── Program.cs                    # DI container, services, logging
```

### Key Components

#### CustomAuthStateProvider
Handles JWT token parsing and authentication state:
- Parses JWT tokens to extract claims (user ID, email, role, name)
- Maps JWT claim types to Blazor's `ClaimTypes` constants
- Handles both full URI claims (`http://schemas.xmlsoap.org/...`) and short forms (`unique_name`, `role`)
- Provides authentication state to Blazor's `<AuthorizeView>` components
- Notifies UI of auth state changes (login/logout)

```csharp
// Automatically updates UI when user logs in/out
public void NotifyAuthenticationStateChanged()
{
    NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
}
```

#### TokenRefreshHandler
Automatically refreshes expired access tokens:
- Intercepts HTTP responses with 401 Unauthorized
- Calls `/auth/refresh` with refresh token
- Stores new access + refresh tokens
- Retries original failed request
- Handles refresh failures gracefully (redirects to login)

```csharp
// Seamless token refresh - user never notices
protected override async Task<HttpResponseMessage> SendAsync(
    HttpRequestMessage request, 
    CancellationToken cancellationToken)
{
    // Add access token to request
    // If 401, refresh and retry
    // If refresh fails, logout
}
```

#### API Clients
Type-safe wrappers around HttpClient for each API endpoint:
- `UserLogInApiClient` - Login, logout, validation
- `UserBookingApiClient` - Booking CRUD
- `UserServiceApiClient` - Service browsing
- `RefreshApiClient` - Token refresh (no handler to avoid circular dependency)

All clients automatically include the access token via `TokenRefreshHandler`.

### Logging

The frontend uses **Serilog** with **BrowserConsole** sink:
- Logs appear in browser DevTools (F12 → Console)
- Structured logging with timestamps and log levels
- Enriched with application and environment context
- Debug-level logging for authentication flows
- Error logging for API failures

**Example Console Output:**
```
[08:53:33 INF] WasmClient.Services.CustomAuthStateProvider: Mapped Name Claim: Charles
[08:53:33 INF] WasmClient.Services.CustomAuthStateProvider: Mapped Role Claim: Admin
[08:53:33 INF] User authenticated - Identity.Name: 'Charles', IsAuthenticated: true, Claims Count: 9
```

---

## Backend API

### API Endpoints

#### Authentication

**Register User**
```http
POST /api/UserRegistration/register
Content-Type: application/json

{
  "firstName": "John",
  "lastName": "Doe",
  "email": "john.doe@example.com",
  "password": "Pass123!@",
  "phoneNumber": "07700900123",
  "role": "Customer"
}
```

**Login**
```http
POST /api/UserLogIn
Content-Type: application/json

{
  "email": "john.doe@example.com",
  "password": "Pass123!@"
}

Response:
{
  "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "email": "john.doe@example.com",
  "expiresIn": 1800,
  "refreshToken": "base64-encoded-refresh-token"
}
```

**Refresh Access Token**
```http
POST /auth/refresh
Content-Type: application/json

{
  "refreshToken": "your-refresh-token"
}

Response:
{
  "accessToken": "new-jwt-token...",
  "email": "john.doe@example.com",
  "expiresIn": 1800,
  "refreshToken": "new-refresh-token"
}
```

**Logout**
```http
POST /api/UserLogIn/logout
Content-Type: application/json

{
  "refreshToken": "your-refresh-token"
}
```

#### Bookings

**Get All Bookings**
```http
GET /api/UserBooking
Authorization: Bearer {access-token}

Response (Customer sees only their bookings):
[
  {
    "id": 1,
    "scheduledStart": "2024-01-26T10:00:00",
    "scheduledEnd": "2024-01-26T11:00:00",
    "status": "Confirmed",
    "lastName": "Doe",
    "email": "john.doe@example.com",
    "serviceName": "Haircut"
  }
]
```

**Create Booking**
```http
POST /api/UserBooking
Authorization: Bearer {access-token}
Content-Type: application/json

{
  "serviceId": 1,
  "scheduledStart": "2024-01-26T10:00:00",
  "scheduledEnd": "2024-01-26T11:00:00",
  "status": "Pending"
}
```
Note: User ID is extracted from JWT token, not request body.

#### Services

**Get All Services**
```http
GET /api/UserService
Authorization: Bearer {access-token}

Response:
[
  {
    "id": 1,
    "serviceName": "Haircut",
    "serviceType": "Barbering",
    "serviceDescription": "Professional haircut service"
  }
]
```

**Create Service (Admin only)**
```http
POST /api/UserService
Authorization: Bearer {access-token}
Content-Type: application/json

{
  "serviceName": "Haircut",
  "serviceType": "Barbering",
  "serviceDescription": "Professional haircut service"
}
```

### Database Schema

**Users**
- Id, FirstName, LastName, Email (unique), PasswordHash, PhoneNumber, Role

**RefreshTokens**
- Id, UserId (FK), Token, CreatedAt, ExpiresAt, IsRevoked

**Services**
- Id, ServiceName, ServiceType, ServiceDescription

**Bookings**
- Id, UserId (FK), ServiceId (FK), ScheduledStart, ScheduledEnd, Status

**Relationships:**
- User 1:N Bookings
- Service 1:N Bookings
- User 1:N RefreshTokens

---

## Testing

The project includes **92 comprehensive tests** with **100% pass rate**.

### Test Coverage

**Unit Tests (77 tests)**
- `UserBookingServiceTests` (31 tests) - Booking CRUD, authorization, conflicts
- `UserLogInServiceTests` (15 tests) - Login, logout, validation
- `UserRegistrationServiceTests` (17 tests) - Registration, validation
- `UserServiceServiceTests` (7 tests) - Service management
- `RefreshServiceTests` (10 tests) - Token refresh, rotation
- `JwtServiceTests` - JWT generation and claims

**Integration Tests (15 tests)**
- End-to-end authentication flows
- Token refresh with rotation
- Booking ownership enforcement
- Service CRUD with authorization
- 401 handling on unauthorized access

### Running Tests

```bash
# Run all tests
cd ServiceBookingPlatformApi
dotnet test

# Run specific test class
dotnet test --filter "FullyQualifiedName~RefreshServiceTests"

# Verbose output
dotnet test --logger "console;verbosity=detailed"
```

**Expected Output:**
```
Starting test execution, please wait...
A total of 1 test files matched the specified pattern.

Passed!  - Failed:     0, Passed:    92, Skipped:     0, Total:    92
```

---

## User Interface

### Pages

**Home (`/`)**
- Landing page with service overview
- Login/Register call-to-action for anonymous users
- Quick navigation for authenticated users

**Login (`/login`)**
- Email + password form
- Client-side validation
- Redirects to home on success
- Displays server error messages

**Services (`/services`)**
- Browse all available services
- Service cards with descriptions
- Protected route (login required)

**My Bookings (`/bookings`)**
- View user's bookings
- Edit/Cancel functionality
- Real-time status updates
- Customers see only their own bookings

**Add Booking (`/bookings/add`)**
- Select service from dropdown
- Pick date/time
- Submit new booking request
- Conflict detection

**Admin Dashboard (`/admin`)**
- View all users
- View all bookings (all customers)
- Manage services (create/edit/delete)
- Admin-only access

### Navigation

**MainLayout.razor**
- Responsive navigation bar
- Conditional menu items based on auth state:
  - Anonymous: Login button
  - Authenticated: Services, My Bookings, Admin (if Admin role), Logout
- User greeting: "Hello, {FirstName}"
- Bootstrap navbar with mobile hamburger menu

---

## Configuration

### API Configuration (`appsettings.json`)

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=.;Database=ServiceBookingPlatform;Trusted_Connection=True;TrustServerCertificate=True"
  },
  "JwtConfig": {
    "Issuer": "https://localhost:7022/",
    "Audience": "https://localhost:7022/",
    "Key": "YOUR_SECRET_KEY_HERE_MIN_32_CHARS",
    "TokenValidityMins": 30,
    "RefreshTokenValidityDays": 7
  },
  "AllowedOrigins": [
    "https://localhost:7234",
    "http://localhost:5261"
  ],
  "Seq": {
    "ServerUrl": "http://localhost:5341",
    "ApiKey": ""
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  }
}
```

### Frontend Configuration (`wwwroot/appsettings.json`)

```json
{
  "ApiBaseUrl": "https://localhost:7022"
}
```

---

## Troubleshooting

### Frontend Issues

**Problem: "Hello, " (name not showing) in navigation**

**Solution:** This was caused by JWT claim type mismatch. Fixed in `CustomAuthStateProvider.cs` by handling both full URI claims and short forms:

```csharp
// Now handles both formats
if (kvp.Key == "unique_name" || 
    kvp.Key == "name" || 
    kvp.Key == "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name")
{
    claims.Add(new Claim(ClaimTypes.Name, kvp.Value.ToString()!));
}
```

**Problem: CORS errors when calling API**

**Solution:** Ensure frontend URL is in API's `AllowedOrigins` in `appsettings.json`.

**Problem: 401 Unauthorized on every request**

**Causes:**
1. Access token expired → Should auto-refresh (check TokenRefreshHandler)
2. Refresh token expired → User needs to login again
3. Token not being sent → Check browser DevTools Network tab for Authorization header

**Problem: Logs not appearing in browser console**

**Solution:** Ensure Serilog.Sinks.BrowserConsole is installed and configured in `Program.cs`:
```bash
dotnet add package Serilog.Sinks.BrowserConsole
```

### API Issues

**Problem: "Sequence contains no elements" in tests**

**Solution:** Seed test data before querying. Example:
```csharp
var user = new User { /* properties */ };
context.Users.Add(user);
await context.SaveChangesAsync();
```

**Problem: Database connection failures**

**Solution:** Verify connection string and ensure SQL Server is running.

---

## Dependencies

### API (ServiceBookingPlatformApi)

```xml
<PackageReference Include="Microsoft.EntityFrameworkCore.SqlServer" Version="10.0.0" />
<PackageReference Include="Microsoft.EntityFrameworkCore.Tools" Version="10.0.0" />
<PackageReference Include="Microsoft.AspNetCore.Authentication.JwtBearer" Version="10.0.0" />
<PackageReference Include="Microsoft.AspNetCore.Identity.EntityFrameworkCore" Version="10.0.0" />
<PackageReference Include="Serilog.AspNetCore" Version="8.0.0" />
<PackageReference Include="Serilog.Sinks.Seq" Version="7.0.0" />
<PackageReference Include="Scalar.AspNetCore" Version="1.0.0" />
<PackageReference Include="xunit" Version="2.6.0" />
```

### Frontend (WasmClient)

```xml
<PackageReference Include="Microsoft.AspNetCore.Components.WebAssembly" Version="10.0.0" />
<PackageReference Include="Microsoft.AspNetCore.Components.WebAssembly.DevServer" Version="10.0.0" />
<PackageReference Include="Microsoft.AspNetCore.Components.Authorization" Version="10.0.0" />
<PackageReference Include="Serilog" Version="3.1.0" />
<PackageReference Include="Serilog.Sinks.BrowserConsole" Version="2.0.0" />
<PackageReference Include="Microsoft.Extensions.Http" Version="10.0.0" />
```

---

## Deployment

### Production Checklist

**API**
- [ ] Update `JwtConfig:Key` to secure random string (32+ chars)
- [ ] Set `RequireHttpsMetadata = true` in JWT configuration
- [ ] Update `ConnectionStrings:DefaultConnection` for production database
- [ ] Update `AllowedOrigins` with production frontend URL
- [ ] Configure Seq `ServerUrl` and `ApiKey` for production
- [ ] Set `ASPNETCORE_ENVIRONMENT=Production`
- [ ] Enable HSTS headers
- [ ] Review rate limiting thresholds
- [ ] Set up database backups
- [ ] Run `dotnet test` to ensure all tests pass

**Frontend**
- [ ] Update `wwwroot/appsettings.json` with production API URL
- [ ] Remove development logging (set to Warning/Error)
- [ ] Enable service worker for PWA (optional)
- [ ] Configure CDN for static assets (optional)
- [ ] Set `ASPNETCORE_ENVIRONMENT=Production`

### Deployment Options

**Azure App Service**
1. Publish API as Azure App Service
2. Publish Frontend as Static Web App
3. Configure Application Insights
4. Set environment variables in Azure Portal

**Docker**
```dockerfile
# API Dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY ./publish .
ENTRYPOINT ["dotnet", "ServiceBookingPlatformApi.dll"]
```

**IIS**
1. Publish API to folder
2. Create IIS site pointing to publish folder
3. Configure Application Pool (.NET CLR Version: No Managed Code)
4. Install .NET 10 Hosting Bundle
5. Publish Frontend to separate folder/site

---

## Contributing

This is a portfolio project, but contributions are welcome!

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/AmazingFeature`)
3. Write tests for your changes
4. Ensure all tests pass (`dotnet test`)
5. Commit your changes (`git commit -m 'Add amazing feature'`)
6. Push to the branch (`git push origin feature/AmazingFeature`)
7. Open a Pull Request

**Please ensure:**
- Code follows existing patterns (interface-based services, DTOs, etc.)
- All tests pass (92/92)
- New features have corresponding tests
- API changes are documented in this README

---
---

## Future Enhancements

**High Priority**
- Email notifications (booking confirmations)
-  Password reset flow
-Remember me / extended sessions
- User profile page (edit name, email, password)

**Medium Priority**
- Service availability calendar
- Customer reviews and ratings
- Booking history and analytics
- Multi-device session management

**Low Priority**
- Payment integration (Stripe)
- Webhook support for third-party integrations
- Admin dashboard charts/graphs
- Dark mode toggle

---

## License

This project is licensed under the MIT License - see the LICENSE file for details.

---

## Author

**sav-afg**

- GitHub: [@sav-afg](https://github.com/sav-afg)
- Repository: [ServiceBookingandManagementPlatform](https://github.com/sav-afg/ServiceBookingandManagementPlatform)

---

## Acknowledgments

- Built with **.NET 10** and **Blazor WebAssembly**
- API documentation powered by **Scalar**
- Authentication with **JWT Bearer** tokens
- Database management with **Entity Framework Core**
- Logging with **Serilog** and **Seq**
- UI built with **Bootstrap 5** and **Bootstrap Icons**
- Testing with **xUnit**

---

**Built using .NET 10, Blazor WebAssembly, and industry best practices**

