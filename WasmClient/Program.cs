using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Serilog.Events;
using WasmClient.Api;
using WasmClient.Handlers;
using WasmClient.Services;

namespace WasmClient
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            // Configure Serilog before anything else
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Information()
                .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
                .MinimumLevel.Override("Microsoft.AspNetCore.Components.WebAssembly", LogEventLevel.Information)
                .Enrich.WithProperty("Application", "WasmClient")
                .WriteTo.BrowserConsole(
                    restrictedToMinimumLevel: LogEventLevel.Debug,
                    outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {SourceContext}: {Message:lj}{NewLine}{Exception}")
                .CreateLogger();

            try
            {
                Log.Information("Starting Blazor WebAssembly application");

                var builder = WebAssemblyHostBuilder.CreateDefault(args);

                // Add environment enrichment after builder is created
                Log.Logger = new LoggerConfiguration()
                    .MinimumLevel.Information()
                    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
                    .MinimumLevel.Override("Microsoft.AspNetCore.Components.WebAssembly", LogEventLevel.Information)
                    .Enrich.WithProperty("Application", "WasmClient")
                    .Enrich.WithProperty("Environment", builder.HostEnvironment.Environment)
                    .WriteTo.BrowserConsole(
                        restrictedToMinimumLevel: LogEventLevel.Debug,
                        outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {SourceContext}: {Message:lj}{NewLine}{Exception}")
                    .CreateLogger();

                builder.Logging.SetMinimumLevel(LogLevel.Information);
                builder.Logging.AddSerilog(Log.Logger, dispose: true);

                builder.RootComponents.Add<App>("#app");
                builder.RootComponents.Add<HeadOutlet>("head::after");

                // Configure base HttpClient with API URL
                var apiBaseUrl = builder.Configuration["ApiBaseUrl"] ?? "https://localhost:7022";
                Log.Information("API Base URL configured: {ApiBaseUrl}", apiBaseUrl);

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

                var host = builder.Build();
                
                Log.Information("Application built successfully, starting host");
                
                await host.RunAsync();
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "Application terminated unexpectedly");
                throw;
            }
            finally
            {
                await Log.CloseAndFlushAsync();
            }
        }
    }
}
