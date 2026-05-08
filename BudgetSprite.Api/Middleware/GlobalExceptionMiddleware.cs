using System.Net;
using System.Text.Json;

namespace BudgetSprite.Api.Middleware;

public class GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext ctx)
    {
        try
        {
            await next(ctx);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unhandled exception");
            ctx.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
            ctx.Response.ContentType = "application/json";
            var body = JsonSerializer.Serialize(new { message = "服务器内部错误，请稍后重试" });
            await ctx.Response.WriteAsync(body);
        }
    }
}
