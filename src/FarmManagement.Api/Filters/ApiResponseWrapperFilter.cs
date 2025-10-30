using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Http;
using System;
using System.Threading.Tasks;

namespace FarmManagement.Api.Filters
{
    public class ApiResponseWrapperFilter : IAsyncResultFilter
    {
    public async Task OnResultExecutionAsync(ResultExecutingContext context, ResultExecutionDelegate next)
        {
            // Don't wrap non-HTTP requests
            if (context == null) { await next(); return; }

            // If result already appears wrapped, skip
            if (context.Result is ObjectResult orObj && orObj.Value != null)
            {
                var valueType = orObj.Value.GetType();
                if (valueType.GetProperty("success") != null)
                {
                    await next();
                    return;
                }
            }

            // Handle different result types
            // correlation id: attempt to read from headers, otherwise create one
            string? correlationId = null;
            if (context.HttpContext.Request.Headers.TryGetValue("X-Correlation-Id", out var headerVal))
            {
                correlationId = headerVal.ToString();
            }
            if (string.IsNullOrWhiteSpace(correlationId))
            {
                correlationId = Guid.NewGuid().ToString();
                // set on response so clients can see it
                context.HttpContext.Response.Headers["X-Correlation-Id"] = correlationId;
            }

            if (context.Result is ObjectResult objectResult)
            {
                var status = objectResult.StatusCode ?? 200;

                if (status >= 400)
                {
                    // Build error payload
                    object errorPayload;

                    if (objectResult.Value is ValidationProblemDetails vpd)
                    {
                        errorPayload = new
                        {
                            code = "VALIDATION_FAILED",
                            message = "Validation failed",
                            details = vpd.Errors
                        };
                    }
                    else if (objectResult.Value is ProblemDetails pd)
                    {
                        errorPayload = new
                        {
                            code = pd.Status == 404 ? "NOT_FOUND" : "ERROR",
                            message = pd.Detail ?? pd.Title ?? "An error occurred"
                        };
                    }
                    else
                    {
                        // If value is a string, use it as message, otherwise attempt to stringify
                        var msg = objectResult.Value as string ?? objectResult.Value?.ToString() ?? "An error occurred";
                        errorPayload = new { code = "ERROR", message = msg };
                    }

                    var wrapped = new
                    {
                        success = false,
                        error = errorPayload,
                        timestamp = DateTime.UtcNow.ToString("o"),
                        correlationId = correlationId
                    };

                    context.Result = new ObjectResult(wrapped) { StatusCode = status };
                }
                else
                {
                    var wrapped = new
                    {
                        success = true,
                        data = objectResult.Value,
                        message = (string?)null,
                        timestamp = DateTime.UtcNow.ToString("o"),
                        correlationId = correlationId
                    };

                    context.Result = new ObjectResult(wrapped) { StatusCode = status };
                }
            }
            else if (context.Result is JsonResult jsonResult)
            {
                var wrapped = new
                {
                    success = true,
                    data = jsonResult.Value,
                    message = (string?)null,
                    timestamp = DateTime.UtcNow.ToString("o"),
                    correlationId = correlationId
                };

                context.Result = new JsonResult(wrapped) { StatusCode = jsonResult.StatusCode ?? 200 };
            }
            else if (context.Result is StatusCodeResult statusResult)
            {
                var code = statusResult.StatusCode;
                if (code >= 400)
                {
                    var wrapped = new
                    {
                        success = false,
                        error = new { code = "ERROR", message = ReasonPhraseFor(code) },
                        timestamp = DateTime.UtcNow.ToString("o"),
                        correlationId = correlationId
                    };
                    context.Result = new ObjectResult(wrapped) { StatusCode = code };
                }
                else
                {
                    var wrapped = new
                    {
                        success = true,
                        data = (object?)null,
                        message = (string?)null,
                        timestamp = DateTime.UtcNow.ToString("o"),
                        correlationId = correlationId
                    };
                    context.Result = new ObjectResult(wrapped) { StatusCode = code };
                }
            }

            await next();
        }

        private static string ReasonPhraseFor(int? statusCode)
        {
            if (!statusCode.HasValue) return "";
            var code = statusCode.Value;
            return code switch
            {
                401 => "Unauthorized",
                403 => "Forbidden",
                404 => "Not Found",
                400 => "Bad Request",
                500 => "Internal Server Error",
                _ => string.Empty
            };
        }
    }
}
