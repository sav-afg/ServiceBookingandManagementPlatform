using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using Scalar.AspNetCore;
using ServiceBookingPlatform.Data;
using ServiceBookingPlatform.Middleware;
using ServiceBookingPlatform.Models.Configuration;
using ServiceBookingPlatform.Services;
using Serilog;
using System.Text;
using System.Threading.RateLimiting;

namespace ServiceBookingPlatform
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Configure Serilog
            builder.Host.UseSerilog((context, configuration) =>
            {
                configuration
                    .ReadFrom.Configuration(context.Configuration)
                    .Enrich.FromLogContext()
                    .Enrich.WithMachineName()
                    .Enrich.WithThreadId()
                    .WriteTo.Console()
                    .WriteTo.Seq(
                        serverUrl: context.Configuration["Seq:ServerUrl"] ?? "http://localhost:5341",
                        apiKey: context.Configuration["Seq:ApiKey"]);
            });

            // strongly-typed configuration
            builder.Services.Configure<JwtConfig>(builder.Configuration.GetSection("JwtConfig"));

            builder.Services.AddControllers();

            // response compression
            builder.Services.AddResponseCompression(options =>
            {
                options.EnableForHttps = true;
            });

            // CORS with proper configuration
            builder.Services.AddCors(options =>
            {
                options.AddPolicy("BlazorWasmPolicy", policy =>
                {
                    policy.WithOrigins(
                        "https://localhost:7234", // Development Blazor URL
                        "http://localhost:5261",  // Development HTTP URL
                        "https://mydomain.com"    // Production Blazor URL
                    )
                    .AllowAnyMethod()
                    .AllowAnyHeader()
                    .AllowCredentials(); // Important for cookies if used
                });
            });

            // Rate limiting
            builder.Services.AddRateLimiter(options =>
            {
                options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
                    RateLimitPartition.GetFixedWindowLimiter(
                        partitionKey: context.User.Identity?.Name ?? context.Request.Headers.Host.ToString(),
                        factory: partition => new FixedWindowRateLimiterOptions
                        {
                            AutoReplenishment = true,
                            PermitLimit = 100,
                            QueueLimit = 0,
                            Window = TimeSpan.FromMinutes(1)
                        }));

                options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            });

            // Configure OpenAPI with JWT Security
            builder.Services.AddOpenApi(options =>
            {
                options.AddDocumentTransformer((document, context, cancellationToken) =>
                {
                    
                    document.Info = new()
                    {
                        Title = "Service Booking Platform API",
                        Version = "v1",
                        Description = "RESTful API for managing service bookings, authentication, and service management",
                        Contact = new() { Name = "Support", Email = "support@servicebooking.com" }
                    };
                    // Create JWT Bearer security scheme
                    var securityScheme = new OpenApiSecurityScheme
                    {
                        Type = SecuritySchemeType.Http,
                        Scheme = "bearer",
                        BearerFormat = "JWT",
                        Description = "JWT Authorization header using the Bearer scheme."
                    };

                    // Initialize Components and SecuritySchemes if null
                    document.Components ??= new OpenApiComponents();

                    if (document.Components.SecuritySchemes == null)
                    {
                        document.Components.SecuritySchemes = new Dictionary<string, IOpenApiSecurityScheme>();
                    }

                    // Add security scheme to document
                    document.Components.SecuritySchemes["Bearer"] = securityScheme;

                    // Create security scheme reference for requirement
                    var schemeReference = new OpenApiSecuritySchemeReference("Bearer", document);

                    // Create and add security requirement to all operations
                    foreach (var path in document.Paths.Values)
                    {
                        foreach (var operation in path.Operations!.Values)
                        {
                            // Initialize Security collection if null
                            operation.Security ??= [];

                            var securityRequirement = new OpenApiSecurityRequirement
                            {
                                [schemeReference] = []
                            };
                            operation.Security.Add(securityRequirement);
                        }
                    }

                    return Task.CompletedTask;
                });
            });

            builder.Services.AddScoped<IUserBookingService, UserBookingService>();
            builder.Services.AddScoped<IUserRegistrationService, UserRegistrationService>();
            builder.Services.AddScoped<IUserLogInService, UserLogInService>();
            builder.Services.AddScoped<IUserServiceService, UserServiceService>();
            builder.Services.AddScoped<IJwtService, JwtService>();
            builder.Services.AddScoped<IRefreshService, RefreshService>();

            /* Database Configuration - Integration Test Support
             Conditionally register database provider based on environment
             When Environment = "Testing", this registration is skipped,
             allowing integration tests to register InMemory database provider
             This prevents the "dual database provider" error in WebApplicationFactory tests*/

            if (builder.Environment.EnvironmentName != "Testing")
            {
                builder.Services.AddDbContext<AppDbContext>(options =>
                {
                    options.UseSqlServer(
                        builder.Configuration.GetConnectionString("DefaultConnection"),
                        sqlServerOptions =>
                        {
                            // Add connection resilience
                            sqlServerOptions.EnableRetryOnFailure(
                                maxRetryCount: 5,
                                maxRetryDelay: TimeSpan.FromSeconds(30),
                                errorNumbersToAdd: null);

                            // Command timeout
                            sqlServerOptions.CommandTimeout(30);
                        });

                    // Enable sensitive data logging only in development
                    if (builder.Environment.IsDevelopment())
                    {
                        options.EnableSensitiveDataLogging();
                        options.EnableDetailedErrors();
                    }
                });
            }
            // If environment is "Testing", the DbContext is registered by test setup with InMemory provider

            builder.Services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
            }).AddJwtBearer(options =>
            {
                options.RequireHttpsMetadata = false;
                options.SaveToken = true;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidIssuer = builder.Configuration["JwtConfig:Issuer"],
                    ValidAudience = builder.Configuration["JwtConfig:Audience"],
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["JwtConfig:Key"]!)),
                    ValidateIssuer = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true
                };

                // Add custom events for logging
                options.Events = new JwtBearerEvents
                {
                    OnAuthenticationFailed = context =>
                    {
                        if (context.Exception is SecurityTokenExpiredException)
                        {
                            context.Response.Headers.Append("Token-Expired", "true");
                        }
                        return Task.CompletedTask;
                    },
                    OnChallenge = context =>
                    {
                        context.HandleResponse();
                        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                        context.Response.ContentType = "application/json";

                        var result = System.Text.Json.JsonSerializer.Serialize(new
                        {
                            message = "You are not authorized to access this resource"
                        });

                        return context.Response.WriteAsync(result);
                    }
                };
            });

            builder.Services.AddAuthorization();

            var app = builder.Build();

            // Add Serilog request logging
            app.UseSerilogRequestLogging(options =>
            {
                options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
                {
                    diagnosticContext.Set("UserName", httpContext.User.Identity?.Name ?? "Anonymous");
                    diagnosticContext.Set("ClientIP", httpContext.Connection.RemoteIpAddress?.ToString());
                };
            });

            // Add global exception handling
            app.UseMiddleware<GlobalExceptionHandlerMiddleware>();

            // Use response compression
            app.UseResponseCompression();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.MapOpenApi();
                app.MapScalarApiReference(options =>
                {
                    options
                        .WithTitle("Service Booking Platform API")
                        .WithTheme(ScalarTheme.Purple)
                        .WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.HttpClient);
                });
            }

            app.UseHttpsRedirection();

            // Add CORS before authentication
            app.UseCors("BlazorWasmPolicy");

            // Add rate limiting
            app.UseRateLimiter();

            app.UseAuthentication();
            app.UseAuthorization();

            app.MapControllers();

            try
            {
                Log.Information("Starting Service Booking Platform API");
                app.Run();
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "Application terminated unexpectedly");
            }
            finally
            {
                Log.CloseAndFlush();
            }
        }
    }
}
