using System.Net;
using System.Text.Json;
using HospitalBillingApi.DTOs.Common;

namespace HospitalBillingApi.Middleware;

public class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;

    public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
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
        catch (KeyNotFoundException knfEx)
        {
            _logger.LogWarning(knfEx, "Resource not found: {Message}", knfEx.Message);
            await HandleExceptionAsync(context, HttpStatusCode.NotFound, knfEx.Message);
        }
        catch (InvalidOperationException invEx)
        {
            _logger.LogWarning(invEx, "Business rule violation: {Message}", invEx.Message);
            await HandleExceptionAsync(context, HttpStatusCode.BadRequest, invEx.Message);
        }
        catch (ArgumentException argEx)
        {
            _logger.LogWarning(argEx, "Invalid argument: {Message}", argEx.Message);
            await HandleExceptionAsync(context, HttpStatusCode.BadRequest, argEx.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unhandled exception occurred: {Message}", ex.Message);
            await HandleExceptionAsync(context, HttpStatusCode.InternalServerError, "An unexpected internal server error occurred.");
        }
    }

    private static async Task HandleExceptionAsync(HttpContext context, HttpStatusCode statusCode, string message)
    {
        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)statusCode;

        var response = ApiResponse<object>.Fail(message, new List<string> { message });
        var json = JsonSerializer.Serialize(response, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        await context.Response.WriteAsync(json);
    }
}
