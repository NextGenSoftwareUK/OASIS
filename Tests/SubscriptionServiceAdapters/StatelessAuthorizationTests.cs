using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Routing;
using Xunit;

public sealed class StatelessAuthorizationTests
{
    [Theory]
    [InlineData(9)] [InlineData(10)]
    public void ValidatedIdentityDoesNotRequireASecondLocalAvatarProjection(int service)
    {
        var id=Guid.NewGuid();var context=Context(id,id,true);
        Filter(service).OnAuthorization(context);
        Assert.Null(context.Result);
    }
    [Theory]
    [InlineData(9)] [InlineData(10)]
    public void ForgedContextIdentityAndMismatchedClaimsAreRejected(int service)
    {
        var id=Guid.NewGuid();var context=Context(id,id,false);
        Filter(service).OnAuthorization(context);Assert.Equal(401,Assert.IsType<JsonResult>(context.Result).StatusCode);
        context=Context(id,Guid.NewGuid(),true);
        Filter(service).OnAuthorization(context);Assert.Equal(401,Assert.IsType<JsonResult>(context.Result).StatusCode);
        context=Context(Guid.Empty,Guid.Empty,true);
        Filter(service).OnAuthorization(context);Assert.Equal(401,Assert.IsType<JsonResult>(context.Result).StatusCode);
    }
    private static IAuthorizationFilter Filter(int service)=>service==9 ? new NextGenSoftware.OASIS.Web9.WebAPI.Attributes.AuthorizeAttribute() : new NextGenSoftware.OASIS.Web10.WebAPI.Attributes.AuthorizeAttribute();
    private static AuthorizationFilterContext Context(Guid contextId,Guid claimId,bool authenticated)
    {
        var http=new DefaultHttpContext();http.Items["AvatarId"]=contextId;
        http.User=new ClaimsPrincipal(new ClaimsIdentity(new[]{new Claim("id",claimId.ToString())},authenticated?"Bearer":null));
        return new AuthorizationFilterContext(new ActionContext(http,new RouteData(),new ActionDescriptor()),new List<IFilterMetadata>());
    }
}
