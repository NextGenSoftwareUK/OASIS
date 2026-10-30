using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Moq;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.UnitTests;

public sealed partial class CosmicDataControllerHttpTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ExplicitPointLoadUsesItsRequestProviderInsteadOfTheManagersDefault(bool selectedFails)
    {
        var id = Guid.NewGuid();
        var selected = new Mock<IOASISStorageProvider>();
        selected.SetupAllProperties();
        selected.Object.ProviderType = new EnumValue<ProviderType>(ProviderType.MongoDBOASIS);
        selected.Object.ProviderCategory = new EnumValue<ProviderCategory>(ProviderCategory.Storage);
        selected.Object.IsProviderActivated = true;

        await WithHost(false, async (client, actor, defaultProvider) =>
        {
            selected.Setup(value => value.LoadHolonAsync(id, false, false, 2, false, true, 0))
                .ReturnsAsync(selectedFails
                    ? new OASISResult<IHolon> { IsError = true, ErrorCode = "CONTROLLED_READ_FAILURE", Message = "Selected provider rejected the read." }
                    : new OASISResult<IHolon>(new Holon { Id = id, CreatedByAvatarId = actor, Name = "selected provider" }) { IsLoaded = true });

            using var response = await client.PostAsJsonAsync("/api/data/load-holon", new
            {
                id, providerType = "MongoDBOASIS", loadChildren = false, recursive = false,
                maxChildDepth = 2, continueOnError = false, loadChildrenFromProvider = true,
                autoFailOverMode = "OFF", autoLoadBalanceMode = "OFF"
            });

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            var result = body.RootElement.GetProperty("result");
            Assert.Equal(selectedFails, result.GetProperty("isError").GetBoolean());
            if (!selectedFails)
                Assert.Equal(id, result.GetProperty("result").GetProperty("id").GetGuid());
            selected.Verify(value => value.LoadHolonAsync(id, false, false, 2, false, true, 0), Times.Once);
            Assert.DoesNotContain(defaultProvider.Invocations, call => call.Method.Name == "LoadHolonAsync");
        }, configureProviders: providers => providers.RegisterProvider(selected.Object));
    }
}
