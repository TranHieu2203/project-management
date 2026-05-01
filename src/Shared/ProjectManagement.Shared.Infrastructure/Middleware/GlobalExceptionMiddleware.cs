using System.Text.Json;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using ProjectManagement.Shared.Domain.Exceptions;

namespace ProjectManagement.Shared.Infrastructure.Middleware;

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
        context.Response.ContentType = "application/problem+json";

        var problem = exception switch
        {
            NotFoundException notFound => BuildProblem(
                StatusCodes.Status404NotFound,
                "Not Found",
                notFound.Message,
                "https://tools.ietf.org/html/rfc7231#section-6.5.4"),

            DomainException domain => BuildProblem(
                StatusCodes.Status422UnprocessableEntity,
                "Business Rule Violation",
                domain.Message,
                "https://tools.ietf.org/html/rfc4918#section-11.2"),

            ConflictException conflict => BuildConflictProblem(context, conflict),

            ForbiddenException forbidden => BuildProblem(
                StatusCodes.Status403Forbidden,
                "Forbidden",
                forbidden.Message,
                "https://tools.ietf.org/html/rfc7231#section-6.5.3"),

            ValidationException validation => BuildValidationProblem(validation),

            ArgumentException arg => BuildProblem(
                StatusCodes.Status400BadRequest,
                "Bad Request",
                arg.Message,
                "https://tools.ietf.org/html/rfc7231#section-6.5.1"),

            _ => BuildInternalErrorProblem(exception)
        };

        context.Response.StatusCode = problem.Status!.Value;

        var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
        await context.Response.WriteAsync(JsonSerializer.Serialize(problem, options));
    }

    private static ProblemDetails BuildProblem(int status, string title, string detail, string type)
    {
        return new ProblemDetails
        {
            Status = status,
            Title = title,
            Detail = detail,
            Type = type
        };
    }

    private static ProblemDetails BuildConflictProblem(HttpContext context, ConflictException conflict)
    {
        var problem = new ProblemDetails
        {
            Status = StatusCodes.Status409Conflict,
            Title = "Conflict",
            Detail = conflict.Message,
            Type = "https://tools.ietf.org/html/rfc7231#section-6.5.8"
        };

        if (conflict.CurrentState is not null)
            problem.Extensions["current"] = conflict.CurrentState;

        if (conflict.CurrentETag is not null)
            problem.Extensions["eTag"] = conflict.CurrentETag;

        return problem;
    }

    private static ProblemDetails BuildValidationProblem(ValidationException validation)
    {
        var errors = validation.Errors
            .GroupBy(e => e.PropertyName)
            .ToDictionary(
                g => g.Key,
                g => g.Select(e => e.ErrorMessage).ToArray());

        var problem = new ProblemDetails
        {
            Status = StatusCodes.Status400BadRequest,
            Title = "Validation Error",
            Detail = "One or more validation errors occurred.",
            Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1"
        };
        problem.Extensions["errors"] = errors;

        return problem;
    }

    private ProblemDetails BuildInternalErrorProblem(Exception exception)
    {
        _logger.LogError(exception, "Unhandled exception: {Message}", exception.Message);

        return new ProblemDetails
        {
            Status = StatusCodes.Status500InternalServerError,
            Title = "Internal Server Error",
            Detail = "An unexpected error occurred. Please try again later.",
            Type = "https://tools.ietf.org/html/rfc7231#section-6.6.1"
        };
    }
}
