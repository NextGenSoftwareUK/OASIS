using CosmicCqlDemo;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.DNA;

namespace NextGenSoftware.OASIS.API.Providers.AzureCosmosDBOASIS.IntegrationTests;

[TestClass]
public sealed class CqlHostedApiLabelTests
{
    [DataTestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public async Task PageLabelsDeclaredEnvironmentWithoutQueryingProviders(bool hosted)
    {
        var dna = new OASISDNA { OASIS = new NextGenSoftware.OASIS.API.DNA.OASIS { HyperDriveMode = HyperDriveModes.V2 } };
        var manager = new HolonManager(null, dna, new ProviderManager(null, dna));
        var mongo = new Mock<ICosmicQueryStorageProvider>(MockBehavior.Strict);
        var cosmos = new Mock<ICosmicQueryStorageProvider>(MockBehavior.Strict);
        var ids = Enumerable.Range(0, 3).Select(_ => Guid.NewGuid()).ToArray();
        await using var app = CloudFederationApiHost.Build(manager, mongo.Object, cosmos.Object,
            Guid.NewGuid(), ids, ids, ids, "http://127.0.0.1:0", hosted: hosted);
        await app.StartAsync();
        try
        {
            var address = app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.Single();
            using var http = new HttpClient { BaseAddress = new Uri(address) };
            var page = await http.GetStringAsync("/");
            StringAssert.Contains(page, hosted ? "hosted Azure Cosmos DB" : "local Cosmos emulator");
            Assert.IsFalse(page.Contains(hosted ? "local Cosmos emulator" : "hosted Azure Cosmos DB"));
            StringAssert.Contains(page, "AzureCosmosDBOASIS");
            mongo.VerifyNoOtherCalls();
            cosmos.VerifyNoOtherCalls();
        }
        finally { await app.StopAsync(); }
    }
}
