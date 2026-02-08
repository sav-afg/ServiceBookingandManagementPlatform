using ServiceBookingPlatform.Models.Dtos.User;

namespace WasmClient.Api
{
    public interface IRefreshApiClient
    {
        Task<UserLogInResponseDto> RefreshAsync(RefreshTokenRequestDto request);
    }

    public class RefreshApiClient(HttpClient httpClient, ILogger<RefreshApiClient> logger) 
        : ApiClientBase(httpClient, logger), IRefreshApiClient
    {
        public async Task<UserLogInResponseDto> RefreshAsync(RefreshTokenRequestDto request)
            => await PostAsync<RefreshTokenRequestDto, UserLogInResponseDto>("api/refresh", request)
                ?? throw new InvalidOperationException("Token refresh failed");
    }
}
