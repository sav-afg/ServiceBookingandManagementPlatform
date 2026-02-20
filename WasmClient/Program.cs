using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.DependencyInjection;
using WasmClient.Api;
using WasmClient.Handlers;
using WasmClient.Services;

namespace WasmClient
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebAssemblyHostBuilder.CreateDefault(args);

            builder.Logging.ClearProviders();

            builder.Logging.AddOpenTelemetry();

            builder.RootComponents.Add<App>("#app");
            builder.RootComponents.Add<HeadOutlet>("head::after");

            // Configure base HttpClient with API URL
            var apiBaseUrl = builder.Configuration["ApiBaseUrl"] ?? "https://localhost:7022";

            // Register TokenService (scoped for IJSRuntime dependency)
            builder.Services.AddScoped<ITokenService, TokenService>();
            
            // Register JSInterop service
            builder.Services.AddScoped<IJSInteropService, JSInteropService>();

            // Register the token refresh handler as transient
            builder.Services.AddTransient<TokenRefreshHandler>();

            // Register API clients with the handler
            builder.Services.AddHttpClient<IUserLogInApiClient, UserLogInApiClient>(client =>
            {
                client.BaseAddress = new Uri(apiBaseUrl);
            })
            .AddHttpMessageHandler<TokenRefreshHandler>();

            builder.Services.AddHttpClient<IUserBookingApiClient, UserBookingApiClient>(client =>
            {
                client.BaseAddress = new Uri(apiBaseUrl);
            })
            .AddHttpMessageHandler<TokenRefreshHandler>();

            builder.Services.AddHttpClient<IUserServiceApiClient, UserServiceApiClient>(client =>
            {
                client.BaseAddress = new Uri(apiBaseUrl);
            })
            .AddHttpMessageHandler<TokenRefreshHandler>();

            // Register RefreshApiClient without the handler to avoid circular refresh
            builder.Services.AddHttpClient<IRefreshApiClient, RefreshApiClient>(client =>
            {
                client.BaseAddress = new Uri(apiBaseUrl);
            });

            // Register authentication services
            builder.Services.AddAuthorizationCore();
            builder.Services.AddScoped<AuthenticationStateProvider, CustomAuthStateProvider>();

            // Default HttpClient for non-API calls
            builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

            await builder.Build().RunAsync();
        }
    }
}
