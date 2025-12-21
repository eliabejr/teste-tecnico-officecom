using System.Net;
using System.Text.Json;
using BCBGames.Application.DTOs;
using BCBGames.Domain.Exceptions;

namespace BCBGames.API.Middleware;

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
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }
    
    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var (statusCode, response) = exception switch
        {
            UnauthorizedAccessException ex => (
                HttpStatusCode.Unauthorized,
                new ErrorResponse(ex.Message, null, 401)),

            AccountNotFoundException ex => (
                HttpStatusCode.NotFound, 
                new ErrorResponse(ex.Message, null, 404)),
            
            TransactionNotFoundException ex => (
                HttpStatusCode.NotFound, 
                new ErrorResponse(ex.Message, null, 404)),
            
            InsufficientBalanceException ex => (
                HttpStatusCode.UnprocessableEntity, 
                new ErrorResponse(ex.Message, null, 422)),
            
            ConcurrencyException ex => (
                HttpStatusCode.Conflict, 
                new ErrorResponse(ex.Message, "Please retry the operation", 409)),
            
            InvalidAmountException ex => (
                HttpStatusCode.BadRequest, 
                new ErrorResponse(ex.Message, null, 400)),
            
            ArgumentException ex => (
                HttpStatusCode.BadRequest, 
                new ErrorResponse(ex.Message, null, 400)),
            
            _ => (
                HttpStatusCode.InternalServerError, 
                new ErrorResponse("An unexpected error occurred", null, 500))
        };
        
        if (statusCode == HttpStatusCode.InternalServerError)
        {
            _logger.LogError(exception, "Unhandled exception occurred");
        }
        else
        {
            _logger.LogWarning("Business exception: {Message}", exception.Message);
        }
        
        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)statusCode;
        
        var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
        await context.Response.WriteAsync(JsonSerializer.Serialize(response, options));
    }
}
