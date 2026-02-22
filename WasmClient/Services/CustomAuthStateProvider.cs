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
                logger.LogInformation("No access token found, returning anonymous user");
                return new AuthenticationState(
                    new ClaimsPrincipal(new ClaimsIdentity()));
            }

            // If we have a token, we can parse it to extract the claims and create an authenticated user
            try
            {
                var claims = ParseClaimsFromJwt(token, logger);
                var identity = new ClaimsIdentity(
                        claims,
                        authenticationType: "jwt",
                        nameType: ClaimTypes.Name,
                        roleType: ClaimTypes.Role);

                var user = new ClaimsPrincipal(identity);

                // Log to verify Identity.Name is set
                logger.LogInformation("User authenticated - Identity.Name: '{IdentityName}', IsAuthenticated: {IsAuthenticated}, Claims Count: {ClaimsCount}",
                    user.Identity?.Name ?? "(null)",
                    user.Identity?.IsAuthenticated ?? false,
                    claims.Count());

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
                // Map role claims (both short form and full URI) to ClaimTypes.Role
                if (kvp.Key == "role" || kvp.Key == "http://schemas.microsoft.com/ws/2008/06/identity/claims/role")
                {
                    claims.Add(new Claim(ClaimTypes.Role, kvp.Value.ToString()!));
                    logger.LogInformation("Mapped Role Claim: {Value}", kvp.Value);
                }
                // Map name claims (JWT short form, standard claim, and full URI) to ClaimTypes.Name
                else if (kvp.Key == "unique_name" || 
                         kvp.Key == "name" || 
                         kvp.Key == "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name")
                {
                    claims.Add(new Claim(ClaimTypes.Name, kvp.Value.ToString()!));
                    logger.LogInformation("Mapped Name Claim: {Value}", kvp.Value);
                }
                else
                {
                    claims.Add(new Claim(kvp.Key, kvp.Value.ToString()!));
                    logger.LogInformation("Added Claim - Type: {Type}, Value: {Value}", kvp.Key, kvp.Value);
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
