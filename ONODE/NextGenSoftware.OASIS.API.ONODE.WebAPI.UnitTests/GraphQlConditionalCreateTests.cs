using HotChocolate;
using HotChocolate.Execution;
using HotChocolate.Types;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.GraphQL;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.UnitTests;

[CollectionDefinition("GraphQL boot configuration", DisableParallelization = true)]
public class GraphQlBootConfigurationCollection { }

[Collection("GraphQL boot configuration")]
public class GraphQlConditionalCreateTests
{
    [Theory]
    [InlineData(false, "COSMIC_WRITE_ACTOR_REQUIRED", false)]
    [InlineData(true, "COSMIC_WRITE_INTENT_INVALID", false)]
    [InlineData(false, "COSMIC_WRITE_ACTOR_REQUIRED", true)]
    [InlineData(true, "COSMIC_EXPECTED_REVISION_REQUIRED", true)]
    public async Task SchemaExecutesShippingSaveResolverAndReturnsConditionalAdmissionErrors(
        bool authenticated, string expectedCode, bool deletion)
    {
        var previous = OASISBootLoader.OASISBootLoader.OASISDNA;
        try
        {
            OASISBootLoader.OASISBootLoader.OASISDNA = new OASISDNA
            {
                OASIS = new NextGenSoftware.OASIS.API.DNA.OASIS { HyperDriveMode = HyperDriveModes.V2 }
            };
            var services = new ServiceCollection();
            var context = new DefaultHttpContext();
            if (authenticated)
                context.Items["Avatar"] = new NextGenSoftware.OASIS.API.Core.Holons.Avatar { Id = Guid.NewGuid() };
            services.AddSingleton<IHttpContextAccessor>(new HttpContextAccessor { HttpContext = context });
            services.AddGraphQLServer()
                .AddQueryType(descriptor => descriptor.Name("Query").Field("ping").Resolve("pong"))
                .AddMutationType(new ObjectType<Mutation>(descriptor =>
                {
                    descriptor.BindFieldsExplicitly();
                    descriptor.Field(value => value.SaveHolon(default!, default!, default!, default!, default, default!, default));
                    descriptor.Field(value => value.DeleteHolon(default, default!, default, default, default, default!));
                }))
                .AddType<NextGenSoftware.OASIS.API.ONODE.WebAPI.GraphQL.Types.HolonType>();
            await using var provider = services.BuildServiceProvider();
            context.RequestServices = provider;
            var executor = await provider.GetRequiredService<IRequestExecutorProvider>().GetExecutorAsync();

            var query = deletion
                ? $"mutation {{ deleteHolon(id: \"{Guid.NewGuid():D}\", operationId: \"{Guid.NewGuid():D}\") }}"
                : "mutation { saveHolon(name: \"coffee\", description: \"demo\", holonType: \"Quest\") { id } }";
            var result = await executor.ExecuteAsync(query);

            Assert.Contains(expectedCode, result.ToJson());
            Assert.DoesNotContain("HC0018", result.ToJson());
        }
        finally { OASISBootLoader.OASISBootLoader.OASISDNA = previous; }
    }
}
