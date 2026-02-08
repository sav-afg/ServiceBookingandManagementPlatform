using Microsoft.JSInterop;

namespace WasmClient.Services
{
    public interface ITokenService
    {
        Task<string?> GetAccessTokenAsync();
        Task<string?> GetRefreshTokenAsync();
        Task SetTokensAsync(string accessToken, string refreshToken);
        Task ClearTokensAsync();
    }
   
    public class TokenService(IJSRuntime jsRuntime) : ITokenService
    {
        public async Task<string?> GetAccessTokenAsync()
            => await jsRuntime.InvokeAsync<string?>("localStorage.getItem", "accessToken");

        public async Task<string?> GetRefreshTokenAsync()
            => await jsRuntime.InvokeAsync<string?>("localStorage.getItem", "refreshToken");

        public async Task SetTokensAsync(string accessToken, string refreshToken)
        {
            await jsRuntime.InvokeVoidAsync("localStorage.setItem", "accessToken", accessToken);
            await jsRuntime.InvokeVoidAsync("localStorage.setItem", "refreshToken", refreshToken);
        }

        public async Task ClearTokensAsync()
        {
            await jsRuntime.InvokeVoidAsync("localStorage.removeItem", "accessToken");
            await jsRuntime.InvokeVoidAsync("localStorage.removeItem", "refreshToken");
        }
    }
}
