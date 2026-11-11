using AMZN.Middleware;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace AMZN.Tests;

public class ApiCancellationTests
{
    // Разрыв соединения не должен превращать отменённый запрос в ошибку 500.
    [Fact]
    public async Task Invoke_WhenClientDisconnects_StopsWithoutErrorBody()
    {
        using var abort = new CancellationTokenSource();
        var context = new DefaultHttpContext();
        context.Request.Path = "/api/products";
        context.RequestAborted = abort.Token;
        using var responseBody = new MemoryStream();
        context.Response.Body = responseBody;

        var middleware = new ApiExceptionMiddleware(httpContext =>
        {
            abort.Cancel();
            httpContext.RequestAborted.ThrowIfCancellationRequested();
            return Task.CompletedTask;
        }, NullLogger<ApiExceptionMiddleware>.Instance);

        await middleware.Invoke(context);

        Assert.Equal(StatusCodes.Status499ClientClosedRequest, context.Response.StatusCode);
        Assert.Equal(0, responseBody.Length);
    }
}
