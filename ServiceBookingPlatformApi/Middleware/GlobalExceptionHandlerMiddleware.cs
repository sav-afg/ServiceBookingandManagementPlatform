using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Text.Json;

namespace ServiceBookingPlatform.Middleware
{
    public class GlobalExceptionHandlerMiddleware(RequestDelegate _next, ILogger<GlobalExceptionHandlerMiddleware> _logger, IHostEnvironment _environment)
    {
        // Cache JsonSerializerOptions
        private static readonly JsonSerializerOptions _jsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        // The middleware captures unhandled exceptions, logs them, and returns a standardized JSON response with appropriate HTTP status codes based on the exception type.
        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unhandled exception occurred: {Message}", ex.Message);
                await HandleExceptionAsync(context, ex);
            }
        }
        // The method maps specific exception types to corresponding HTTP status codes and constructs a ProblemDetails response.
        // In development mode, it includes additional details like the stack trace for easier debugging.
        private async Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            context.Response.ContentType = "application/json";

            var problemDetails = new ProblemDetails();

            switch (exception)
            {
                case UnauthorizedAccessException:
                    context.Response.StatusCode = (int)HttpStatusCode.Forbidden;
                    problemDetails.Status = (int)HttpStatusCode.Forbidden;
                    problemDetails.Title = "Forbidden";
                    problemDetails.Detail = exception.Message;
                    break;

                case ArgumentException:
                case InvalidOperationException:
                    context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                    problemDetails.Status = (int)HttpStatusCode.BadRequest;
                    problemDetails.Title = "Bad Request";
                    problemDetails.Detail = exception.Message;
                    break;

                case KeyNotFoundException:
                    context.Response.StatusCode = (int)HttpStatusCode.NotFound;
                    problemDetails.Status = (int)HttpStatusCode.NotFound;
                    problemDetails.Title = "Not Found";
                    problemDetails.Detail = exception.Message;
                    break;

                default:
                    context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                    problemDetails.Status = (int)HttpStatusCode.InternalServerError;
                    problemDetails.Title = "Internal Server Error";
                    problemDetails.Detail = _environment.IsDevelopment()
                        ? exception.Message
                        : "An error occurred while processing your request.";
                    break;
            }

            problemDetails.Instance = context.Request.Path;

            // Add trace ID for debugging
            if (_environment.IsDevelopment())
            {
                problemDetails.Extensions["traceId"] = context.TraceIdentifier;
                problemDetails.Extensions["stackTrace"] = exception.StackTrace;
            }

            var json = JsonSerializer.Serialize(problemDetails, _jsonOptions);
            await context.Response.WriteAsync(json);
        }

    }

}
