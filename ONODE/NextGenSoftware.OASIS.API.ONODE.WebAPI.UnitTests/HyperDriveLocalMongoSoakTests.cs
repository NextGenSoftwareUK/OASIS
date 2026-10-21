using System.Diagnostics;
using System.Reflection;
using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using MongoDB.Bson;
using MongoDB.Driver;
using NextGenSoftware.OASIS.API.Core.Configuration;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers.OASISHyperDrive.Synchronization;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.HyperDrive;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.UnitTests;

[CollectionDefinition("Local HyperDrive soak", DisableParallelization = true)]
public sealed class LocalHyperDriveSoakCollection { }

[Collection("Local HyperDrive soak")]
public sealed class HyperDriveLocalMongoSoakTests
{
    [Fact]
    [Trait("Category", "LocalMongoSoak")] // Needs the operator's loopback MongoDB; run via Scripts/Test-HyperDriveLocalWorkers.ps1. Hosted CI filters this category out.
    public async Task LocalWorkersBackOffWithoutIdleLeaseWritesAndKeepClaimsExclusive()
    {
        const string connection="mongodb://127.0.0.1:27017/?directConnection=true&serverSelectionTimeoutMS=3000";
        int seconds=int.TryParse(Environment.GetEnvironmentVariable("OASIS_LOCAL_WORKER_SOAK_SECONDS"),out var configured) ? configured : 12;
        Assert.InRange(seconds,5,3600);
        var client=new MongoClient(connection);
        string name="hd_soak_"+Guid.NewGuid().ToString("N")[..20];
        var database=client.GetDatabase(name);
        var prior=OASISDNAManager.OASISDNA;
        var dna=new OASISDNA();
        dna.OASIS.HyperDriveMode=HyperDriveModes.V2;
        dna.OASIS.OASISHyperDriveConfig=new OASISHyperDriveConfig{EnableHostedSync=true};
        var provider=new NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.MongoDBOASIS(connection,name);
        HyperDriveCommandHostedService? worker=null;
        using var cancel=new CancellationTokenSource();
        Task? fanOut=null;
        var queryTimes=new List<double>();
        int fanOutPolls=0;
        var started=DateTime.UtcNow;
        try
        {
            var active=await provider.ActivateProviderAsync();
            Assert.False(active.IsError,active.Message);
            var initialized=await provider.ClaimCommandsAsync("initialize",1,DateTime.UtcNow.AddMinutes(5),default);
            Assert.False(initialized.IsError,initialized.Message);
            // Profiling is confined to this owned disposable database, never a server/global setting.
            await database.RunCommandAsync<BsonDocument>(new BsonDocument{{"profile",2},{"slowms",0}});
            OASISDNAManager.OASISDNA=dna;
            var accessor=new HyperDriveHostedProviderAccessor();
            // Seed the production accessor cache; bypass boot/config loading so Atlas cannot be selected.
            typeof(HyperDriveHostedProviderAccessor).GetField("_initialization",BindingFlags.Instance|BindingFlags.NonPublic)!
                .SetValue(accessor,Task.FromResult(new OASISResult<IOASISStorageProvider>{Result=provider}));
            worker=new HyperDriveCommandHostedService(NullLogger<HyperDriveCommandHostedService>.Instance,accessor);
            await worker.StartAsync(cancel.Token);
            var dispatcher=new HostedHyperDriveFanOutDispatcher(provider,
                new OASISProviderFanOutHandler(Array.Empty<IHyperDriveIdempotentReplicationTarget>()),"soak-fanout");
            fanOut=Task.Run(async()=>{
                var backoff=new HyperDriveWorkerBackoff();
                while(!cancel.IsCancellationRequested)
                {
                    var result=await dispatcher.DispatchOnceAsync(25,TimeSpan.FromMinutes(2),cancel.Token);
                    Assert.False(result.IsError,result.Message);
                    Interlocked.Increment(ref fanOutPolls);
                    var delay=backoff.NextDelay(result.Result.ClaimedCount>0);
                    if(delay>TimeSpan.Zero)await Task.Delay(delay,cancel.Token);
                }
            });
            var watch=Stopwatch.StartNew();
            while(watch.Elapsed<TimeSpan.FromSeconds(seconds))
            {
                var query=Stopwatch.StartNew();
                await database.GetCollection<BsonDocument>("HyperDriveSyncCommands")
                    .Find(new BsonDocument("_id","local-latency-probe")).Limit(1).ToListAsync();
                queryTimes.Add(query.Elapsed.TotalMilliseconds);
                await Task.Delay(TimeSpan.FromSeconds(2));
            }
            // Measure steady-state polling separately from the worker's shutdown lease release.
            await database.RunCommandAsync<BsonDocument>(new BsonDocument("profile",0));
            await worker.StopAsync(CancellationToken.None);
            cancel.Cancel();
            try{await fanOut;}catch(OperationCanceledException)when(cancel.IsCancellationRequested){}
            await database.RunCommandAsync<BsonDocument>(new BsonDocument("profile",0));
            var profile=await database.GetCollection<BsonDocument>("system.profile").Find(new BsonDocument()).ToListAsync();
            int leaseWrites=profile.Count(x=>x.GetValue("ns","").AsString.Contains("Leases",StringComparison.Ordinal) &&
                (x.GetValue("op","").AsString is "update" or "insert" || x.GetValue("command",new BsonDocument()).AsBsonDocument.Contains("findAndModify")));
            int commandPolls=profile.Count(x=>x.GetValue("command",new BsonDocument()).AsBsonDocument.TryGetValue("find",out var find) &&
                find=="HyperDriveSyncCommands" && !x["command"].AsBsonDocument.GetValue("filter",new BsonDocument()).AsBsonDocument.Contains("_id"));
            Assert.Equal(0,leaseWrites);
            int maximumPolls=6+(int)Math.Ceiling(Math.Max(0,seconds-31)/30d);
            Assert.InRange(commandPolls,1,maximumPolls);
            Assert.InRange(fanOutPolls,1,maximumPolls);
            Assert.NotNull(worker.ExecuteTask);
            Assert.False(worker.ExecuteTask.IsFaulted);

            // Queue a diagnostic record and race two real native claimers, without executing domain commands.
            var operation=Guid.NewGuid();
            await database.GetCollection<BsonDocument>("HyperDriveSyncCommands").InsertOneAsync(new BsonDocument {
                {"_id",operation.ToString("D")},{"operationId",operation.ToString("D")},
                {"avatarId",Guid.NewGuid().ToString("D")},{"deviceId",Guid.NewGuid().ToString("D")},
                {"deviceSequence",1L},{"commandSequence",1L},{"entityId",Guid.NewGuid().ToString("D")},
                {"entityType","local-soak-diagnostic"},{"versionId",Guid.NewGuid().ToString("D")},{"payloadJson","{}"},
                {"status","pending"},{"nextAttemptUtc",DateTime.UtcNow.AddSeconds(-1)},
                {"leaseOwner",BsonNull.Value},{"leaseUntilUtc",BsonNull.Value},{"attemptCount",0}
            });
            var claims=await Task.WhenAll(provider.ClaimCommandsAsync("race-a",1,DateTime.UtcNow.AddMinutes(1),default),
                provider.ClaimCommandsAsync("race-b",1,DateTime.UtcNow.AddMinutes(1),default));
            Assert.All(claims,x=>Assert.False(x.IsError,x.Message));
            Assert.Equal(1,claims.Sum(x=>x.Result.Items.Count));
            queryTimes.Sort();
            var report=new {StartedUtc=started,DurationSeconds=seconds,LoopbackOnly=true,RealCommandHostedService=true,
                RealFanOutDispatcher=true,CommandPolls=commandPolls,FanOutPolls=fanOutPolls,IdleLeaseWrites=leaseWrites,
                LatencySamples=queryTimes.Count,MedianQueryMilliseconds=queryTimes[queryTimes.Count/2],
                MaximumQueryMilliseconds=queryTimes.Last(),ExclusiveClaimRacePassed=true,
                Scope="Idle scheduling and native claim exclusivity; no full command execution, replica-set capture or external replication"};
            var reportPath=Environment.GetEnvironmentVariable("OASIS_LOCAL_WORKER_SOAK_REPORT");
            if(!string.IsNullOrWhiteSpace(reportPath))await System.IO.File.WriteAllTextAsync(reportPath,JsonSerializer.Serialize(report,new JsonSerializerOptions{WriteIndented=true}));
        }
        finally
        {
            try
            {
                try { if(worker!=null)await worker.StopAsync(CancellationToken.None); }
                finally
                {
                    cancel.Cancel();
                    if(fanOut!=null){try{await fanOut;}catch(OperationCanceledException)when(cancel.IsCancellationRequested){}}
                }
            }
            finally
            {
                worker?.Dispose();
                OASISDNAManager.OASISDNA=prior;
                try { await database.RunCommandAsync<BsonDocument>(new BsonDocument("profile",0)); }
                finally { await client.DropDatabaseAsync(name); }
            }
        }
    }
}
