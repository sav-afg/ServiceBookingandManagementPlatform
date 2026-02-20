using ServiceBookingPlatform.Models.Dtos.User;

namespace WasmClient.Api
{
    public interface IUserLogInApiClient
    {
        Task<UserLogInResponseDto> LoginAsync(UserLogInRequestDto request);
    }

    public class UserLogInApiClient(HttpClient httpClient, ILogger<UserLogInApiClient> logger) : ApiClientBase(httpClient, logger), IUserLogInApiClient
    {
        public async Task<UserLogInResponseDto> LoginAsync(UserLogInRequestDto request)
        {
            var result = await PostAsync<UserLogInRequestDto, UserLogInResponseDto>("api/userlogin", request);
            
            if (result == null)
            {
                logger.LogError("Login failed: Received null response from api/userlogin");
                throw new InvalidOperationException("Login failed");
            }
            
            return result;
        }
    }
}
