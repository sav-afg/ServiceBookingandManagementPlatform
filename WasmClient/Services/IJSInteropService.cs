using Microsoft.JSInterop;

namespace WasmClient.Services
{
    // This service provides methods to interact with JavaScript functions from Blazor WebAssembly.
    // It includes methods for displaying confirmation dialogs and alert messages using JavaScript's built-in functions.
    // The IJSInteropService interface defines the contract for the service, while the JSInteropService class implements the interface using the IJSRuntime to invoke JavaScript functions.

    public interface IJSInteropService
    {
        Task<bool> ConfirmAsync(string message);
        Task AlertAsync(string message);
    }

    // The JSInteropService class implements the IJSInteropService interface and uses the IJSRuntime to call JavaScript functions.
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
