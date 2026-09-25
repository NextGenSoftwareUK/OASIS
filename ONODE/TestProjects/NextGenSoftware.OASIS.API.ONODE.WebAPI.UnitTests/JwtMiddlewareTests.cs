using Microsoft.AspNetCore.Http;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Middleware;
using Xunit;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.UnitTests;

public sealed class JwtMiddlewareTests
{
    [Fact]
    public async Task InvalidBearer_RecordsAuthenticationFailureWithoutMutatingResponse()
    {
        bool nextCalled = false;
        var middleware = new JwtMiddleware(_ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });
        var context = new DefaultHttpContext();
        context.Request.Headers.Authorization = "Bearer definitely-not-a-jwt";

        await middleware.Invoke(context);

        Assert.True(nextCalled);
        Assert.Equal(StatusCodes.Status200OK, context.Response.StatusCode);
        Assert.True(context.Items.ContainsKey(JwtMiddleware.AuthenticationErrorItemKey));
        Assert.Null(context.Items["Avatar"]);
    }
}
