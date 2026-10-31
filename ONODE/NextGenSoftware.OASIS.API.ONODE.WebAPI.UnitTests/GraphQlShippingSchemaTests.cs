using HotChocolate;
using HotChocolate.Execution;
using Microsoft.Extensions.DependencyInjection;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.GraphQL;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.UnitTests;

public class GraphQlShippingSchemaTests
{
    /// <summary>
    /// The shipping WEB4 host builds its GraphQL schema at startup. A public Holon property whose type has no
    /// GraphQL fields (such as the runtime-only metadata encryption policy) makes the whole schema fail and the
    /// service crash on boot, so the same registration Startup uses must build, and must not expose that policy.
    /// </summary>
    [Fact]
    public async Task ShippingSchemaBuildsAndDoesNotExposeRuntimeOnlyEncryptionPolicy()
    {
        var services = new ServiceCollection();
        services.AddGraphQLServer()
            .DisableIntrospection(false)
            .AddType(new HotChocolate.Types.AnyType("JSON"))
            .BindRuntimeType<Dictionary<string, object>>("JSON")
            .AddQueryType<Query>()
            .AddMutationType<Mutation>()
            .AddType<GraphQL.Types.AvatarType>()
            .AddType<GraphQL.Types.HolonType>()
            .AddOasisDataTypes()
            .TryAddTypeInterceptor<OASISMethodFieldTypeInterceptor>();

        await using var provider = services.BuildServiceProvider();
        var executor = await provider.GetRequiredService<IRequestExecutorProvider>().GetExecutorAsync();

        var schema = executor.Schema.ToString();
        Assert.DoesNotContain("IHolonMetadataKeyResolver", schema);
        Assert.DoesNotContain("HolonMetadataEncryptionPolicy", schema);
        Assert.DoesNotContain("metadataEncryptionOverride", schema, StringComparison.OrdinalIgnoreCase);
    }
}
