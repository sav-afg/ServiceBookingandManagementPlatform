using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
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
            try
            {
                var builder = WebAssemblyHostBuilder.CreateDefault(args);

                // Suppress logs entirely in production — important events are
                // logged server-side via the API's Serilog → Seq pipeline.
                // In development, show Warning+ in the browser console only.
                var minimumLevel = builder.HostEnvironment.IsProduction()
                    ? LogEventLevel.Fatal   // effectively silent in production
                    : LogEventLevel.Warning;

                Log.Logger = new LoggerConfiguration()
                    .MinimumLevel.Is(minimumLevel)
                    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
                    .Enrich.WithProperty("Application", "WasmClient")
                    .Enrich.WithProperty("Environment", builder.HostEnvironment.Environment)
                    .WriteTo.BrowserConsole(
                        outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {SourceContext}: {Message:lj}{NewLine}{Exception}")
                    .CreateLogger();

                builder.Logging.SetMinimumLevel(LogLevel.Warning);
                builder.Logging.AddSerilog(Log.Logger, dispose: true);

                builder.RootComponents.Add<App>("#app");
                builder.RootComponents.Add<HeadOutlet>("head::after");

                var apiBaseUrl = builder.Configuration["ApiBaseUrl"] ?? "https://localhost:7022";

                builder.Services.AddScoped<ITokenService, TokenService>();
                builder.Services.AddScoped<IJSInteropService, JSInteropService>();
                builder.Services.AddTransient<TokenRefreshHandler>();

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

                // No TokenRefreshHandler here — avoids circular refresh calls
                builder.Services.AddHttpClient<IRefreshApiClient, RefreshApiClient>(client =>
                {
                    client.BaseAddress = new Uri(apiBaseUrl);
                });

                builder.Services.AddAuthorizationCore();
                builder.Services.AddScoped<AuthenticationStateProvider, CustomAuthStateProvider>();
                builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

                await builder.Build().RunAsync();
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
