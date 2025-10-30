using Microsoft.AspNetCore.Http;
using System;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;

namespace FarmManagement.Api.Middleware
{
    public class ApiExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly Microsoft.Extensions.Logging.ILogger<ApiExceptionMiddleware> _logger;

        public ApiExceptionMiddleware(RequestDelegate next, Microsoft.Extensions.Logging.ILogger<ApiExceptionMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Unhandled exception processing request {Method} {Path}", context.Request?.Method, context.Request?.Path.Value);
                await HandleExceptionAsync(context, ex);
            }
        }

        private Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            // Default to 500
            var code = (int)HttpStatusCode.InternalServerError;

            // Map some known exceptions to other codes
            if (exception is ArgumentException) code = (int)HttpStatusCode.BadRequest;

            // correlation id: prefer header, otherwise generate
            string correlationId = context.Request.Headers.ContainsKey("X-Correlation-Id")
                ? context.Request.Headers["X-Correlation-Id"].ToString()
                : Guid.NewGuid().ToString();
            context.Response.Headers["X-Correlation-Id"] = correlationId;

            var payload = new
            {
                success = false,
                error = new
                {
                    code = exception is ArgumentException ? "BAD_REQUEST" : "UNHANDLED_ERROR",
                    message = exception.Message
                },
                timestamp = DateTime.UtcNow.ToString("o"),
                correlationId = correlationId
            };

            var json = JsonSerializer.Serialize(payload);
            context.Response.ContentType = "application/json";
            context.Response.StatusCode = code;
            return context.Response.WriteAsync(json);
        }
    }

    public static class ApiExceptionMiddlewareExtensions
    {
        public static IApplicationBuilder UseApiExceptionMiddleware(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<ApiExceptionMiddleware>();
        }
    }
}
