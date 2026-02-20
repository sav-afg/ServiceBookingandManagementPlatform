using Microsoft.AspNetCore.Components.Authorization;
using System.Security.Claims;
using System.Text.Json;

namespace WasmClient.Services
{
    public class CustomAuthStateProvider(ITokenService tokenService, ILogger<CustomAuthStateProvider> logger) : AuthenticationStateProvider
    {
        public override async Task<AuthenticationState> GetAuthenticationStateAsync()
        {
        
            var token = await tokenService.GetAccessTokenAsync();

            if (string.IsNullOrEmpty(token))
            {
                return new AuthenticationState(
                    new ClaimsPrincipal(new ClaimsIdentity()));
            }

            // If we have a token, we can parse it to extract the claims and create an authenticated user
            try
            {
                var claims = ParseClaimsFromJwt(token, logger);
                var identity = new ClaimsIdentity(claims, "jwt");
                var user = new ClaimsPrincipal(identity);
                return new AuthenticationState(user);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to parse JWT");
                return new AuthenticationState(
                    new ClaimsPrincipal(new ClaimsIdentity()));
            }
        }

        public void NotifyAuthenticationStateChanged()
        {
            NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
        }

        private static IEnumerable<Claim> ParseClaimsFromJwt(string jwt, ILogger<CustomAuthStateProvider> logger)
        {
            var payload = jwt.Split('.')[1];
            var jsonBytes = ParseBase64WithoutPadding(payload);
            var keyValuePairs = JsonSerializer.Deserialize<Dictionary<string, object>>(jsonBytes);

            logger.LogDebug(keyValuePairs != null
                ? $"Parsed claims from JWT: {string.Join(", ", keyValuePairs.Select(kvp => $"{kvp.Key}={kvp.Value}"))}"
                : "No claims found in JWT.");

            var claims = new List<Claim>();

            foreach (var kvp in keyValuePairs!)
            {
                // Map the role claim to the standard claim type that Blazor expects
                if (kvp.Key == "http://schemas.microsoft.com/ws/2008/06/identity/claims/role")
                {
                    claims.Add(new Claim(ClaimTypes.Role, kvp.Value.ToString()!));
                }
                // Map the name claim to the standard claim type that Blazor expects
                else if (kvp.Key == "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name")
                {
                    claims.Add(new Claim(ClaimTypes.Name, kvp.Value.ToString()!));
                }
                else
                {
                    claims.Add(new Claim(kvp.Key, kvp.Value.ToString()!));
                }
            }

            return claims;
        }

        private static byte[] ParseBase64WithoutPadding(string base64)
        {
            switch (base64.Length % 4)
            {
                case 2: base64 += "=="; break;
                case 3: base64 += "="; break;
            }
            return Convert.FromBase64String(base64);
        }
    }
}
