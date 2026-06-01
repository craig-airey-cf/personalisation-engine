using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using PersonalisationEngine.Api.Middleware;

namespace PersonalisationEngine.Tests.Unit;

public class GlobalExceptionHandlerTests
{
    private static (GlobalExceptionHandler handler, DefaultHttpContext ctx) Build(Exception toThrow)
    {
        RequestDelegate next = _ => throw toThrow;
        var handler = new GlobalExceptionHandler(next, NullLogger<GlobalExceptionHandler>.Instance);
        var ctx = new DefaultHttpContext();
        ctx.Response.Body = new MemoryStream();
        return (handler, ctx);
    }

    private static async Task<string> ReadBodyAsync(DefaultHttpContext ctx)
    {
        ctx.Response.Body.Seek(0, SeekOrigin.Begin);
        return await new StreamReader(ctx.Response.Body).ReadToEndAsync();
    }

    [Fact]
    public async Task NotFoundException_Returns404()
    {
        var (handler, ctx) = Build(new NotFoundException("not found"));
        await handler.InvokeAsync(ctx);
        Assert.Equal(StatusCodes.Status404NotFound, ctx.Response.StatusCode);
        Assert.Contains("not found", await ReadBodyAsync(ctx));
    }

    [Fact]
    public async Task ConflictException_Returns409()
    {
        var (handler, ctx) = Build(new ConflictException("conflict"));
        await handler.InvokeAsync(ctx);
        Assert.Equal(StatusCodes.Status409Conflict, ctx.Response.StatusCode);
    }

    [Fact]
    public async Task BadRequestException_Returns400()
    {
        var (handler, ctx) = Build(new BadRequestException("bad input"));
        await handler.InvokeAsync(ctx);
        Assert.Equal(StatusCodes.Status400BadRequest, ctx.Response.StatusCode);
    }

    [Fact]
    public async Task UnhandledException_Returns500WithGenericMessage()
    {
        var (handler, ctx) = Build(new InvalidOperationException("internal"));
        await handler.InvokeAsync(ctx);
        Assert.Equal(StatusCodes.Status500InternalServerError, ctx.Response.StatusCode);
        var body = await ReadBodyAsync(ctx);
        Assert.Contains("unexpected error", body);
        Assert.DoesNotContain("internal", body);
    }

    [Fact]
    public async Task NoException_PassesThrough()
    {
        RequestDelegate next = ctx => { ctx.Response.StatusCode = 200; return Task.CompletedTask; };
        var handler = new GlobalExceptionHandler(next, NullLogger<GlobalExceptionHandler>.Instance);
        var ctx = new DefaultHttpContext();
        ctx.Response.Body = new MemoryStream();

        await handler.InvokeAsync(ctx);

        Assert.Equal(200, ctx.Response.StatusCode);
    }
}
