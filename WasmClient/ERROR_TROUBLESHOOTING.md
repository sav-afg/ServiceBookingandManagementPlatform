# ?? Error Troubleshooting Guide

## The Error UI Won't Go Away - Here's How to Fix It

### ? Changes Made

1. **Fixed the `confirm()` method** - It was calling a non-existent C# method
   - Created `IJSInteropService` for proper JavaScript interop
   - Updated `Bookings.razor` to use async confirmation dialogs

2. **Made Error UI Dismissible**
   - Added JavaScript to handle the dismiss button click
   - Added global error handlers to log errors to console

3. **Added Error Boundary**
   - Created `AppErrorBoundary.razor` component
   - Wrapped App.razor with error boundary for better error recovery

---

## ?? How to Diagnose the Error

### Step 1: Open Browser Developer Tools

1. Press **F12** in your browser
2. Go to the **Console** tab
3. Look for red error messages
4. The error will tell you exactly what's wrong

### Step 2: Common Errors & Solutions

#### Error: "Cannot read property 'confirm' of undefined"
**Cause:** The old `confirm()` method in Bookings.razor wasn't using JSInterop

**Solution:** ? Already fixed! The new code uses `IJSInteropService`

---

#### Error: "Failed to fetch" or "Network request failed"
**Cause:** API is not running or CORS issue

**Solution:**
1. Make sure your API is running on `https://localhost:7022`
2. Check API's console for errors
3. Verify CORS policy includes `https://localhost:7234`

---

#### Error: "401 Unauthorized"
**Cause:** JWT token expired or invalid

**Solution:**
1. Logout and login again
2. Check browser's Local Storage (F12 ? Application ? Local Storage)
3. Verify `accessToken` and `refreshToken` exist
4. If they're missing, the token refresh handler might have failed

---

#### Error: "Cannot read property of null"
**Cause:** Trying to access a property on a null object

**Solution:**
1. Check the error stack trace to see which component/line
2. Add null checks: `@if (item != null) { ... }`
3. Use null-conditional operators: `item?.Property`

---

#### Error: "JSException: Cannot find 'confirm' on window"
**Cause:** JSInterop trying to call JavaScript before it's ready

**Solution:** ? Already fixed! The new `JSInteropService` properly waits for Blazor to initialize

---

### Step 3: Clear the Error and Test

Now that we've fixed the issues:

1. **Hard Refresh** your browser:
   - Windows: `Ctrl + Shift + R`
   - Mac: `Cmd + Shift + R`

2. **Clear Local Storage**:
   - F12 ? Application ? Local Storage ? Right-click ? Clear

3. **Restart Both Projects**:
   ```bash
   # Terminal 1 - API
   cd ServiceBookingPlatform
   dotnet run
   
   # Terminal 2 - Blazor
   cd WasmClient
   dotnet run
   ```

4. **Test the Error UI Dismiss**:
   - If you still see the error, click the **X button** to dismiss it
   - Check browser console for the actual error message

---

## ??? Manual Error Dismissal

If the error UI is stuck and won't dismiss:

### Option 1: Use Browser Console
```javascript
document.getElementById('blazor-error-ui').style.display = 'none';
```

### Option 2: Hard Reload
Press `Ctrl + Shift + R` (Windows) or `Cmd + Shift + R` (Mac)

### Option 3: Clear Browser Cache
1. F12 ? Network tab
2. Check "Disable cache"
3. Refresh page

---

## ?? Check Your Setup

### Verify API is Running
```bash
curl https://localhost:7022/api/userservice
```
**Expected:** Should return JSON list of services (or 401 if auth required)

### Verify Blazor App Loads
Open: `https://localhost:7234`

**Expected:** Should see login page or home page

### Verify CORS
Check API's console output:
```
info: Microsoft.AspNetCore.Cors.Infrastructure.CorsService[2]
      CORS policy execution successful.
```

If you see CORS errors, the API needs to allow your Blazor app's origin.

---

## ?? Error Logging

### Enable Detailed Logging

Add this to your `wwwroot/appsettings.Development.json`:
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft": "Warning",
      "Microsoft.AspNetCore": "Warning"
    }
  }
}
```

### View Blazor Logs in Console

Press F12 ? Console, then look for:
- ? `Blazor started successfully`
- ? Any red error messages
- ?? Yellow warnings (might indicate issues)

---

## ?? Common Blazor WebAssembly Errors

### 1. "Could not find 'BlazorWasmApi' in 'window'"
**Fix:** Make sure Bootstrap JS is loaded before Blazor

### 2. "Error: Cannot read property 'invokeMethodAsync' of null"
**Fix:** Component trying to call .NET method that doesn't exist

### 3. "System.InvalidOperationException: JavaScript interop calls cannot be issued at this time"
**Fix:** Trying to use JSInterop in `OnInitialized` - use `OnAfterRenderAsync` instead

### 4. "Error: There was an unhandled exception on the current circuit"
**Fix:** Check the error message in console - usually a null reference or type mismatch

---

## ? What We Fixed

### Before:
```csharp
private bool confirm(string message)
{
    // This was wrong - calling non-existent C# method
    return true;
}
```

### After:
```csharp
@inject IJSInteropService JSInterop

private async Task DeleteBooking(int id)
{
    var confirmed = await JSInterop.ConfirmAsync("Are you sure?");
    if (confirmed)
    {
        // Delete logic
    }
}
```

---

## ?? Next Steps

1. **Clear the error** - Click the X button or hard refresh
2. **Check browser console** - See what the actual error was
3. **Test the app** - Try logging in, viewing bookings
4. **Share the error** - If it persists, share the console error message

---

## ?? Still Having Issues?

If the error persists:

1. **Copy the exact error message** from browser console (F12)
2. **Check which page/component** is causing the error
3. **Share the error** - I can help diagnose specific errors

### Error Format to Share:
```
Error: [Copy full error message here]
Component: [Which page - Login, Bookings, etc.]
Action: [What you were doing when it happened]
```

---

## ? Error Prevention Tips

1. **Always use null checks**: `@if (item != null)`
2. **Use try-catch** in async methods
3. **Check API responses** before using data
4. **Use error boundaries** for graceful failures
5. **Test delete operations** after the JSInterop fix

---

**The error UI should now be dismissible! Click the X button to close it, then check the browser console to see what the original error was.**
