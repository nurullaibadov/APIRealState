using System.Net;
using System.Text.Json;

namespace RealEstateAPI.API.Middleware
{
    public class GlobalExceptionHandler
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<GlobalExceptionHandler> _logger;
        private readonly IWebHostEnvironment _env;

        public GlobalExceptionHandler(
            RequestDelegate next,
            ILogger<GlobalExceptionHandler> logger,
            IWebHostEnvironment env)
        {
            _next = next;
            _logger = logger;
            _env = env;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                await HandleExceptionAsync(context, ex);
            }
        }

        private async Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            // Log exception
            _logger.LogError(exception, "An unhandled exception occurred");

            // Response setup
            context.Response.ContentType = "application/json";

            var statusCode = HttpStatusCode.InternalServerError;
            var message = "An error occurred while processing your request";
            var errors = new List<string>();

            // Exception type'a göre response özelleştir
            switch (exception)
            {
                case ArgumentNullException argNullEx:
                    statusCode = HttpStatusCode.BadRequest;
                    message = "Invalid input";
                    errors.Add(argNullEx.Message);
                    break;

                case ArgumentException argEx:
                    statusCode = HttpStatusCode.BadRequest;
                    message = "Invalid argument";
                    errors.Add(argEx.Message);
                    break;

                case UnauthorizedAccessException:
                    statusCode = HttpStatusCode.Unauthorized;
                    message = "Unauthorized access";
                    break;

                case KeyNotFoundException:
                    statusCode = HttpStatusCode.NotFound;
                    message = "Resource not found";
                    break;

                default:
                    // Generic error
                    errors.Add(_env.IsDevelopment()
                        ? exception.Message
                        : "An unexpected error occurred");
                    break;
            }

            context.Response.StatusCode = (int)statusCode;

            var errorResponse = new ErrorResponse
            {
                Success = false,
                Message = message,
                StatusCode = (int)statusCode,
                Errors = errors,
                Timestamp = DateTime.UtcNow
            };

            // Development'ta stack trace ekle
            if (_env.IsDevelopment())
            {
                errorResponse.StackTrace = exception.StackTrace;
                errorResponse.InnerException = exception.InnerException?.Message;
            }

            var options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };

            var json = JsonSerializer.Serialize(errorResponse, options);
            await context.Response.WriteAsync(json);
        }
    }

    /// <summary>
    /// Standardize edilmiş error response
    /// </summary>
    public class ErrorResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public int StatusCode { get; set; }
        public List<string> Errors { get; set; } = new();
        public DateTime Timestamp { get; set; }
        public string? StackTrace { get; set; }
        public string? InnerException { get; set; }
    }

    /// <summary>
    /// Middleware extension method
    /// </summary>
    public static class GlobalExceptionHandlerExtensions
    {
        public static IApplicationBuilder UseGlobalExceptionHandler(this IApplicationBuilder app)
        {
            return app.UseMiddleware<GlobalExceptionHandler>();
        }
    }

}
