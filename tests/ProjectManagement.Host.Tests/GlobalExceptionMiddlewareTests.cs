using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using ProjectManagement.Shared.Domain.Exceptions;
using ProjectManagement.Shared.Infrastructure.Middleware;

namespace ProjectManagement.Host.Tests;

public sealed class GlobalExceptionMiddlewareTests
{
    private static async Task<(int StatusCode, JsonDocument Body)> InvokeMiddleware(Exception exception)
    {
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        var middleware = new GlobalExceptionMiddleware(
            _ => throw exception,
            NullLogger<GlobalExceptionMiddleware>.Instance);

        await middleware.InvokeAsync(context);

        context.Response.Body.Seek(0, SeekOrigin.Begin);
        var body = await JsonDocument.ParseAsync(context.Response.Body);

        return (context.Response.StatusCode, body);
    }

    [Fact]
    public async Task NotFoundException_Returns404()
    {
        var (status, body) = await InvokeMiddleware(new NotFoundException("Project", Guid.NewGuid()));

        Assert.Equal(404, status);
        Assert.Equal(404, body.RootElement.GetProperty("status").GetInt32());
    }

    [Fact]
    public async Task DomainException_Returns422()
    {
        var (status, body) = await InvokeMiddleware(new DomainException("Business rule violated"));

        Assert.Equal(422, status);
        Assert.Equal(422, body.RootElement.GetProperty("status").GetInt32());
    }

    [Fact]
    public async Task ConflictException_Returns409WithExtensions()
    {
        var currentState = new { id = Guid.NewGuid(), name = "Server Version" };
        var ex = new ConflictException("Optimistic lock conflict", currentState, "\"2\"");

        var (status, body) = await InvokeMiddleware(ex);

        Assert.Equal(409, status);
        Assert.Equal(409, body.RootElement.GetProperty("status").GetInt32());
        // ProblemDetails.Extensions has [JsonExtensionData] — keys appear at root level
        Assert.True(body.RootElement.TryGetProperty("current", out _));
    }

    [Fact]
    public async Task GenericException_Returns500WithGenericMessage()
    {
        var (status, body) = await InvokeMiddleware(new InvalidOperationException("sensitive detail"));

        Assert.Equal(500, status);
        var detail = body.RootElement.GetProperty("detail").GetString();
        Assert.DoesNotContain("sensitive detail", detail ?? "");
    }
}
