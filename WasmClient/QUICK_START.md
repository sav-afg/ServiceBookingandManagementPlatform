# ?? Quick Start Guide - Blazor WebAssembly Client

## Running the Application

### 1. Start API Backend
```bash
cd ServiceBookingPlatform
dotnet run
```
? Running on: **https://localhost:7022**

### 2. Start Blazor Client
```bash
cd WasmClient
dotnet run
```
? Running on: **https://localhost:7234**

### 3. Access the App
Open browser: **https://localhost:7234**

---

## ?? Project Structure (What Was Built)

```
WasmClient/
??? Api/                    # HTTP clients for API communication
??? Components/             # Reusable UI components  
??? Handlers/               # HTTP middleware (token refresh)
??? Layout/                 # Navigation & layout
??? Pages/                  # Routable pages
?   ??? Home.razor         # Landing page
?   ??? Login.razor        # Authentication
?   ??? Bookings.razor     # Manage bookings
?   ??? Services.razor     # Browse services
??? Services/               # Business logic
?   ??? TokenService.cs            # JWT storage
?   ??? CustomAuthStateProvider.cs # Auth state
??? Program.cs              # Service registration
```

---

## ?? How Authentication Works

### Login Flow
1. User enters email/password on `/login`
2. POST to `https://localhost:7022/api/userlogin`
3. Receive JWT tokens (access + refresh)
4. Store in browser localStorage
5. Redirect to home page

### Making Authenticated Requests
```
User clicks "My Bookings"
  ?
TokenRefreshHandler intercepts request
  ?
Attaches: Authorization: Bearer {token}
  ?
GET https://localhost:7022/api/userbooking
  ?
API validates JWT ? returns bookings
```

### Auto Token Refresh (on 401)
```
API returns 401 (token expired)
  ?
TokenRefreshHandler catches 401
  ?
POST /api/refresh with refresh token
  ?
Get new access + refresh tokens
  ?
Update localStorage
  ?
Retry original request ? Success!
```

---

## ?? Key Features Implemented

### ? Pages
- **Home** (`/`) - Landing page with auth-aware content
- **Login** (`/login`) - Email/password authentication
- **Bookings** (`/bookings`) - View/manage user bookings (protected)
- **Services** (`/services`) - Browse available services

### ? API Integration
- User login
- Token refresh
- Booking CRUD (Create, Read, Update, Delete)
- Service listing

### ? Security
- JWT authentication
- Automatic token refresh
- Protected routes (require login)
- Token storage in localStorage

---

## ?? Testing the App

### Test Login
1. Open https://localhost:7234/login
2. Enter credentials from your database
3. Click "Login"
4. Should redirect to home page
5. Navigation bar shows "Hello, {username}" and "Logout" button

### Test Protected Routes
1. **Without logging in:** Navigate to /bookings
   - Should redirect to /login
2. **After logging in:** Navigate to /bookings
   - Should show your bookings

### Test Token Refresh
1. Login successfully
2. Wait 15 minutes (or change JWT expiry to 1 minute for testing)
3. Click "My Bookings"
4. Open DevTools ? Network tab
5. Should see automatic POST to `/api/refresh`
6. Page loads successfully without manual re-login

---

## ?? Configuration

### Change API URL
Edit `WasmClient/wwwroot/appsettings.json`:
```json
{
  "ApiBaseUrl": "https://your-api-url.com"
}
```

### Change Blazor App Port
Edit `WasmClient/Properties/launchSettings.json`:
```json
{
  "https": {
    "applicationUrl": "https://localhost:YOUR_PORT"
  }
}
```

**Remember:** Update API's CORS to include your new Blazor URL!

---

## ?? Troubleshooting

### CORS Error
**Error:** "Access to fetch has been blocked by CORS"

**Fix:** In `ServiceBookingPlatform/Program.cs`, verify:
```csharp
policy.WithOrigins(
    "https://localhost:7234",  // Must match Blazor URL
    "http://localhost:5261"
)
```

Then restart both projects.

---

### 401 Unauthorized
**Error:** Login works but all other requests fail with 401

**Fix:**
1. Open browser DevTools (F12)
2. Application tab ? Local Storage ? https://localhost:7234
3. Check if `accessToken` exists
4. Network tab ? check if requests have `Authorization` header
5. If missing, clear localStorage and login again

---

### No Bookings Showing
**Possible causes:**
1. User has no bookings in database
2. Token doesn't have correct user ID
3. API service layer filters bookings incorrectly

**Debug:**
1. Check API logs for the request
2. Check browser console for errors
3. Test API directly: `GET https://localhost:7022/api/userbooking` with Bearer token

---

## ?? Adding New Features

### Add a New Page
```razor
@page "/mypage"
@using WasmClient.Api

<h3>My New Page</h3>

@code {
    protected override async Task OnInitializedAsync()
    {
        // Your logic here
    }
}
```

### Add Navigation Link
Edit `WasmClient/Layout/MainLayout.razor`:
```html
<li class="nav-item">
    <a class="nav-link" href="/mypage">My Page</a>
</li>
```

### Make Page Protected
Add this attribute:
```razor
@using Microsoft.AspNetCore.Authorization
@attribute [Authorize]
```

---

## ?? Important Files to Know

| File | Purpose |
|------|---------|
| `Program.cs` | Service registration & DI container |
| `App.razor` | Root component with routing |
| `MainLayout.razor` | Navigation bar & layout |
| `_Imports.razor` | Global using directives |
| `TokenService.cs` | Manage JWT tokens in localStorage |
| `TokenRefreshHandler.cs` | Auto-attach tokens & refresh on 401 |
| `CustomAuthStateProvider.cs` | Parse JWT & manage auth state |

---

## ?? You're Ready!

### What You Can Do Now:
? Login to the Blazor app  
? View your bookings  
? Browse available services  
? Auto token refresh prevents session timeouts  
? Secure routes with `[Authorize]` attribute  

### Next Steps:
1. Create booking form (`Pages/CreateBooking.razor`)
2. Add user registration page
3. Implement admin features (if you're an admin)
4. Add more UI enhancements (toasts, modals, etc.)

---

**Need Help?** Check:
- `IMPLEMENTATION_SUMMARY.md` - Detailed technical documentation
- `README.md` - Architecture & best practices
- Browser DevTools ? Console & Network tabs for debugging

?? **Happy coding!**
