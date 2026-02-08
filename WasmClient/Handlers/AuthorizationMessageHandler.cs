using ServiceBookingPlatform.Models.Dtos.User;
using System.Net;
using System.Net.Http.Headers;
using WasmClient.Api;
using WasmClient.Services;

namespace WasmClient.Handlers
{
    public class TokenRefreshHandler(ITokenService tokenService, IRefreshApiClient refreshClient, ILogger<TokenRefreshHandler> logger) : DelegatingHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            // Attach access token
            var accessToken = await tokenService.GetAccessTokenAsync();

            if (!string.IsNullOrEmpty(accessToken))
            {
                request.Headers.Authorization =
                    new AuthenticationHeaderValue("Bearer", accessToken);
            }

            var response = await base.SendAsync(request, cancellationToken);

            // Handle 401 by refreshing token
            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                var refreshToken = await tokenService.GetRefreshTokenAsync();
                if (!string.IsNullOrEmpty(refreshToken))
                {
                    try
                    {
                        var refreshResponse = await refreshClient.RefreshAsync(
                            new RefreshTokenRequestDto(refreshToken));

                        await tokenService.SetTokensAsync(
                            refreshResponse.AccessToken,
                            refreshResponse.RefreshToken);

                        // Retry original request with new token
                        request.Headers.Authorization =
                            new AuthenticationHeaderValue("Bearer", refreshResponse.AccessToken);
                        response = await base.SendAsync(request, cancellationToken);
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Token refresh failed");
                        await tokenService.ClearTokensAsync();
                    }
                }
            }

            return response;
        }
    }
}

