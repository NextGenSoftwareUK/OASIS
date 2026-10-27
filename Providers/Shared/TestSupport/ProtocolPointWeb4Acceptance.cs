using Xunit;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.API.Core.Services.Subscriptions;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Controllers;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Cql;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.Providers.TestSupport;

internal static class ProtocolPointWeb4Acceptance
{
    private sealed class Providers(IOASISStorageProvider provider,HolonManager manager,ProviderType authority) : ICqlV1Providers
    {
        public int Lookups;
        public IOASISStorageProvider Resolve(ProviderType? requested)
        {
            Lookups++;
            if(requested!=authority||!provider.IsProviderActivated)throw new InvalidOperationException("Only the explicitly selected active protocol adapter is eligible.");
            return provider;
        }
        public HolonManager CreateManager(IOASISStorageProvider selected)
            =>ReferenceEquals(provider,selected)?manager:throw new InvalidOperationException("Provider substitution rejected.");
    }
    private sealed class Catalogue(Guid actor,Guid record,ProviderType selected) : ICqlReadAccessPolicy
    {
        public OASISResult<CosmicQueryReadAccess> Authorize(Guid authenticatedActor,ProviderType authority)
        {
            if(actor!=authenticatedActor||authority!=selected)throw new InvalidOperationException("Actor or authority differs.");
            return CosmicQueryReadAccess.AuthoritativeRecords(actor,authority,new[]{record});
        }
    }

    internal static async Task RunAsync(IOASISStorageProvider provider,HolonManager manager,Guid actor,string metadataKey,string nativeKey,string sourceMetadataKey,string sourceUri,Guid recordId)
    {
        var authority=((ICosmicQueryStorageProvider)provider).QueryProviderType;
        var providers=new Providers(provider,manager,authority);
        var receipts=new ConcurrentBag<UsageProviderReceipt>();
        var builder=WebApplication.CreateBuilder(new WebApplicationOptions {ApplicationName=typeof(CqlV1Controller).Assembly.FullName,Args=Array.Empty<string>()});
        builder.Logging.ClearProviders();builder.WebHost.UseUrls("http://127.0.0.1:0");
        builder.Services.AddSingleton<ICqlV1Providers>(providers);
        builder.Services.AddSingleton<ICqlReadAccessPolicy>(new Catalogue(actor,recordId,authority));
        builder.Services.AddSingleton(new CqlReadPolicyRequirement(true));
        builder.Services.AddSingleton<CqlV1ResourceFilter>();
        builder.Services.AddControllers().AddApplicationPart(typeof(CqlV1Controller).Assembly);
        await using var app=builder.Build();
        app.Use(async(http,next)=>{
            if(http.Request.Headers.Authorization!="Bearer controlled-protocol-cql"){await next();return;}
            // Controlled identity/reservation/outbox fixture; production JWT and durable billing are separate gates.
            var store=new Mock<IUsageOutboxStore>();
            store.Setup(s=>s.AppendReceiptAsync(It.IsAny<string>(),It.IsAny<string>(),It.IsAny<UsageProviderReceipt>(),It.IsAny<CancellationToken>()))
                .Callback((string id,string owner,UsageProviderReceipt receipt,CancellationToken token)=>{receipt.Validate();receipts.Add(receipt);})
                .Returns(Task.CompletedTask);
            var handle=new UsageExecutionHandle(new UsageOutboxOperation {
                Id=Guid.NewGuid().ToString("D"),UserId=actor.ToString("D"),ExecutionOwner="protocol-http-test",
                Policy=new UsageEndpointPolicy {RequiresProviderMeasurement=true}
            },store.Object,new Web4UsageLedgerOptions(),new SubscriptionAuthorizationResult(),Mock.Of<IUsageReservationAuthority>());
            http.User=new ClaimsPrincipal(new ClaimsIdentity(new[]{new Claim(ClaimTypes.NameIdentifier,actor.ToString("D"))},"controlled-test"));
            http.Items["Avatar"]=new Avatar {Id=actor};
            http.Items[Web4UsageMeasurement.ExecutionKey]=handle;
            var prepared=new PreparedStorageExecution(handle,"protocol-point-native-http",new Dictionary<ProviderType,string>(),
                new[]{authority},new[]{new StorageQueryTariff(authority,0.001m,"protocol-test-v1")});
            await prepared.RunAsync(async()=>{await next();return true;});
        });
        app.MapControllers();await app.StartAsync();
        try
        {
            var address=app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.Single();
            using var client=new HttpClient {BaseAddress=new Uri(address),Timeout=TimeSpan.FromSeconds(15)};
            using var anonymous=await client.PostAsJsonAsync("/api/data/query-cql",new{});
            Assert.Equal(HttpStatusCode.Unauthorized,anonymous.StatusCode);Assert.Equal(0,providers.Lookups);
            client.DefaultRequestHeaders.Authorization=new("Bearer","controlled-protocol-cql");
            using var forged=await client.PostAsync("/api/data/query-cql",new StringContent("{\"actorId\":\"forged\"}",Encoding.UTF8,"application/json"));
            Assert.Equal(HttpStatusCode.BadRequest,forged.StatusCode);Assert.Equal(0,providers.Lookups);

            var filters=new[]{new{MetadataPath=new[]{metadataKey},Scalar="String",Operator="Equal",Value=nativeKey}};
            using var unsupported=await client.PostAsJsonAsync("/api/data/query-cql",new{Provider=authority.ToString(),OrderBy="Name",Take=1,Filters=filters});
            Assert.Equal(HttpStatusCode.BadRequest,unsupported.StatusCode);Assert.Empty(receipts);
            using var scalar=await client.PostAsJsonAsync("/api/data/query-cql/scalar",new {
                Provider=authority.ToString(),OrderBy="None",Operation="LongCount",Filters=filters});
            Assert.Equal(HttpStatusCode.BadRequest,scalar.StatusCode);Assert.Empty(receipts);
            using var projection=await client.PostAsJsonAsync("/api/data/query-cql/projection",new {
                Provider=authority.ToString(),OrderBy="None",Take=1,Filters=filters,
                Columns=new[]{new{Alias="name",Field="Name"}}});
            Assert.Equal(HttpStatusCode.BadRequest,projection.StatusCode);Assert.Empty(receipts);
            using var rows=await client.PostAsJsonAsync("/api/data/query-cql",new{Provider=authority.ToString(),OrderBy="None",Take=1,IncludeMetadata=true,Filters=filters});
            var body=await rows.Content.ReadAsStringAsync();
            Assert.True(rows.StatusCode==HttpStatusCode.OK,body);
            Assert.DoesNotContain("providerUniqueStorageKey",body,StringComparison.OrdinalIgnoreCase);
            using var json=JsonDocument.Parse(body);
            var result=json.RootElement.GetProperty("result");
            Assert.Equal(1,result.GetProperty("count").GetInt32());
            var row=Assert.Single(result.GetProperty("rows").EnumerateArray());
            Assert.Equal(nativeKey,row.GetProperty("metadata").GetProperty(metadataKey).GetString());
            Assert.Equal(sourceUri,row.GetProperty("metadata").GetProperty(sourceMetadataKey).GetString());
            var receipt=Assert.Single(receipts);
            Assert.Equal(authority.ToString(),receipt.Provider);Assert.Equal(1,receipt.Units);
            Assert.Equal(0.001m,receipt.ActualCostUsd);Assert.Equal("protocol-test-v1",receipt.PricingCatalogueVersion);
        }
        finally {await app.StopAsync();}
    }
}
