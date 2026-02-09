# Admin Access Issue - Troubleshooting Guide

## Problem
Admin users unable to access `/services/add` endpoint despite having Admin role.

## Root Cause
The JWT role claim was being stored with the full URI (`http://schemas.microsoft.com/ws/2008/06/identity/claims/role`) instead of the simple claim type that Blazor's authorization system expects.

## Solution Applied

### 1. Fixed CustomAuthStateProvider.cs
Updated the `ParseClaimsFromJwt` method to properly map role claims:

```csharp
private static IEnumerable<Claim> ParseClaimsFromJwt(string jwt)
{
    var payload = jwt.Split('.')[1];
    var jsonBytes = ParseBase64WithoutPadding(payload);
    var keyValuePairs = JsonSerializer.Deserialize<Dictionary<string, object>>(jsonBytes);

    var claims = new List<Claim>();

    foreach (var kvp in keyValuePairs!)
    {
        // Map the role claim to the standard claim type that Blazor expects
        if (kvp.Key == "http://schemas.microsoft.com/ws/2008/06/identity/claims/role")
        {
            claims.Add(new Claim(ClaimTypes.Role, kvp.Value.ToString()!));
        }
        else
        {
            claims.Add(new Claim(kvp.Key, kvp.Value.ToString()!));
        }
    }

    return claims;
}
```

### 2. Created Debug Page
Created `/debug/auth` page to help verify authentication and role claims.

## Steps to Fix Your Issue

### Step 1: Stop and Restart Your Application
Since you're currently debugging, you need to:
1. **Stop the debugger** (Hot reload may not apply this change properly)
2. **Rebuild the WasmClient project**
3. **Restart both the API and Blazor WASM projects**

### Step 2: Log Out and Log Back In
Your current JWT token was issued before the fix, so:
1. Log out of the application
2. Log back in with your admin credentials
3. This will generate a new JWT token

### Step 3: Verify Your Role
Navigate to: `https://localhost:7234/debug/auth`

You should see:
- ? "Is Admin: YES ?"
- ? "Can Access /services/add: YES ?"
- A role claim highlighted in yellow showing your role

### Step 4: Test Access
Try accessing `/services/add` - it should now work!

## Additional Checks

### Verify Your User Has Admin Role in Database
Run this SQL query:
```sql
SELECT Id, Email, Role FROM Users WHERE Email = 'your-admin-email@example.com'
```

The `Role` column should show `0` (Admin), `1` (Staff), or `2` (Customer).

### Check JWT Token Expiration
If you've been logged in for a while:
1. Your token may have expired
2. Log out and log back in to get a fresh token
3. Tokens expire based on `JwtConfig:TokenValidityMins` in appsettings.json

### Verify Authorization Attribute
The AddService.razor page has:
```csharp
@attribute [Authorize(Roles = "Admin")]
```

This is correct - it requires the "Admin" role.

## Common Issues

### Issue: Still Can't Access After Fix
**Solution:** Make sure you:
1. Stopped debugging completely
2. Rebuilt the solution
3. Logged out and back in (to get new JWT)
4. Cleared browser cache/cookies if needed

### Issue: Debug Page Shows "Is Admin: NO"
**Solutions:**
1. Check your user's role in the database
2. Log out and log back in
3. Verify the JWT token contains the role claim

### Issue: Role Shows Correctly But Still Denied
**Solutions:**
1. Clear browser local storage: `localStorage.clear()` in browser console
2. Log out and log back in
3. Restart both API and Blazor projects

## Testing Other Roles

The authorization system now properly supports:
- **Admin** - Can access all admin-only pages
- **Staff** - Can access staff-level features
- **Customer** - Standard user access

## Files Modified
- ? `..\WasmClient\Services\CustomAuthStateProvider.cs` - Fixed role claim parsing
- ? `..\WasmClient\Pages\AuthDebug.razor` - Added debug page

## Next Steps

1. **Stop your debugger**
2. **Rebuild the solution**
3. **Restart both projects**
4. **Log out and log back in**
5. **Visit `/debug/auth` to verify your role**
6. **Try accessing `/services/add`**

The issue should now be resolved! ??
