using System.Net.Http;
using System.Text;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Serialization;

public sealed partial class ReplicaBaselineRepairCommandTests
{
    [NativeFact] public Task LostTransportAckMongoAuthority() => LostCommittedAck(ProviderType.MongoDBOASIS, transport: true);
    [NativeFact] public Task LostTransportAckSqliteAuthority() => LostCommittedAck(ProviderType.SQLLiteDBOASIS, transport: true);
    [NativeFact] public Task LostTransportAckNeoAuthority() => LostCommittedAck(ProviderType.Neo4jOASIS, transport: true);

    // This controlled loopback transport proves an actual post-commit disconnect.
    // It is not the production WEB4 auth/billing/controller or a native DB link fault.
    private static async Task LoseCommittedHttpAcknowledgement(
        HolonManager manager, Holon caller, Guid actor, Guid operation, ProviderType authority)
    {
        var builder = WebApplication.CreateBuilder();
        builder.Logging.ClearProviders();
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        await using var app = builder.Build();
        var completed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var requests = 0;

        app.MapPost("/owned-save", async context =>
        {
            try
            {
                Assert.Equal(1, Interlocked.Increment(ref requests));
                using var body = new StreamReader(context.Request.Body);
                var submitted = StoredRecordJson.DeserializeStoredHolon(await body.ReadToEndAsync());
                Assert.Equal(caller.Id, submitted.Id);
                Assert.Equal(0, submitted.Version);

                // The real manager and native provider commit once, including the
                // two replication obligations, before any response reaches HttpClient.
                var saved = await manager.SaveOwnedHolonAsync(submitted, actor,
                    new CosmicWriteOptions(operation), saveChildren: false, providerType: authority);
                Assert.False(saved.IsError, saved.Message + saved.Exception);
                Assert.True(saved.IsSaved);
                Assert.Equal(1, saved.Result.Version);

                context.Abort();
                completed.SetResult();
            }
            catch (Exception error)
            {
                completed.TrySetException(error);
                throw;
            }
        });

        await app.StartAsync();
        try
        {
            var address = app.Services.GetRequiredService<IServer>()
                .Features.Get<IServerAddressesFeature>()!.Addresses.Single();
            using var client = new HttpClient { BaseAddress = new Uri(address), Timeout = TimeSpan.FromSeconds(30) };
            using var body = new StringContent(StoredRecordJson.SerializeInput(caller), Encoding.UTF8, "application/json");

            // No retry handler, replacement operation or second POST is installed.
            await Assert.ThrowsAsync<HttpRequestException>(() => client.PostAsync("/owned-save", body));
            await completed.Task.WaitAsync(TimeSpan.FromSeconds(30));
            Assert.Equal(1, requests);
        }
        finally
        {
            await app.StopAsync();
        }
    }
}
