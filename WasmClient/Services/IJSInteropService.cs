using Microsoft.JSInterop;

namespace WasmClient.Services
{
    public interface IJSInteropService
    {
        Task<bool> ConfirmAsync(string message);
        Task AlertAsync(string message);
    }

    public class JSInteropService(IJSRuntime jsRuntime) : IJSInteropService
    {
        public async Task<bool> ConfirmAsync(string message)
        {
            return await jsRuntime.InvokeAsync<bool>("confirm", message);
        }

        public async Task AlertAsync(string message)
        {
            await jsRuntime.InvokeVoidAsync("alert", message);
        }
    }
}
