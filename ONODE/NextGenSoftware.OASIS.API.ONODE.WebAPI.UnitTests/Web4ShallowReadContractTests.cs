using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Controllers;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Models.Data;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.UnitTests;

public sealed class Web4ShallowReadContractTests
{
    [Fact]
    public void ShallowReadContractDefaultsGetRouteParametersToFalse()
    {
        var routeParameters = typeof(DataController).GetMethods()
            .Where(method => method.IsDefined(typeof(HttpGetAttribute), inherit: true))
            .SelectMany(method => method.GetParameters())
            .Where(parameter => parameter.Name == "loadChildren")
            .ToArray();
        Assert.NotEmpty(routeParameters);
        Assert.All(routeParameters, parameter =>
        {
            Assert.True(parameter.HasDefaultValue);
            Assert.False(Assert.IsType<bool>(parameter.DefaultValue));
        });
    }

    [Theory]
    [InlineData(typeof(LoadHolonRequest))]
    [InlineData(typeof(LoadAllHolonsRequest))]
    [InlineData(typeof(LoadHolonsForParentRequest))]
    [InlineData(typeof(LoadHolonByMetaDataRequest))]
    [InlineData(typeof(LoadHolonsByMetaDataRequest))]
    [InlineData(typeof(SearchHolonsRequest))]
    public void ShallowReadContractDefaultsJsonBodyAndKeepsExplicitOptIn(Type requestType)
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        var omitted = Assert.IsAssignableFrom<BaseLoadHolonRequest>(JsonSerializer.Deserialize("{}", requestType, options));
        var explicitChildren = Assert.IsAssignableFrom<BaseLoadHolonRequest>(
            JsonSerializer.Deserialize("{\"loadChildren\":true}", requestType, options));
        Assert.False(omitted.LoadChildren);
        Assert.True(explicitChildren.LoadChildren);
    }
}
