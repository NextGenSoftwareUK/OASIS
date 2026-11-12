using System.Net;
using System.Net.Sockets;
using System.Text.Json;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using System.Security.Cryptography;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Managers.OASISHyperDrive;
using NextGenSoftware.OASIS.API.DNA;
using SqliteProvider = NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.SQLLiteDBOASIS;
using AwsS3Provider = NextGenSoftware.OASIS.API.Providers.AWSS3OASIS.AWSS3OASIS;
using NextGenSoftware.Utilities;

namespace NextGenSoftware.OASIS.API.Providers.AWSOASIS.UnitTests;

[TestClass, DoNotParallelize]
public sealed class AwsS3SqliteHyperDriveTests
{
    [DataTestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public async Task RoutesAcrossNativeS3AndSqlite(bool sync)
    {
        var fixture = Environment.GetEnvironmentVariable("OASIS_S3_ROUTING_DIRECTORY");
        Assert.AreEqual("true", Environment.GetEnvironmentVariable("GITHUB_ACTIONS"));
        Assert.IsFalse(string.IsNullOrWhiteSpace(fixture));
        fixture = Path.GetFullPath(fixture!);
        Assert.AreEqual(Path.GetFullPath(Environment.GetEnvironmentVariable("RUNNER_TEMP")!), Path.GetDirectoryName(fixture));
        StringAssert.StartsWith(Path.GetFileName(fixture), "oasis-priority-s3.");
        const string endpoint = "http://127.0.0.1:19000";
        const string bucket = "oasis-routing";
        var accessKey = Environment.GetEnvironmentVariable("OASIS_S3_ROUTING_ACCESS");
        var secretKey = Environment.GetEnvironmentVariable("OASIS_S3_ROUTING_SECRET");
        Assert.IsFalse(string.IsNullOrWhiteSpace(accessKey) || string.IsNullOrWhiteSpace(secretKey));

        AmazonS3Client NativeClient(string url) => new(new BasicAWSCredentials(accessKey, secretKey),
            new AmazonS3Config { ServiceURL = url, ForcePathStyle = true, AuthenticationRegion = "us-east-1",
                MaxErrorRetry = 0, Timeout = TimeSpan.FromSeconds(3) });

        var sqlitePath = Path.Combine(fixture, sync ? "routing-sync.db" : "routing-async.db");
        Assert.IsFalse(File.Exists(sqlitePath));
        using var sqlite = new SqliteProvider($"Data Source={sqlitePath};Pooling=False");
        using var client = NativeClient(endpoint);
        var before = await client.ListObjectsV2Async(new ListObjectsV2Request { BucketName = bucket, MaxKeys = 1 });
        Assert.AreEqual(0, before.S3Objects.Count);
        var key = RandomNumberGenerator.GetBytes(32);
        using var s3 = new AwsS3Provider(client, bucket, () => key, "routing/");
        var attempted = new List<Guid>();
        try
        {
            var s3Activation = await s3.ActivateProviderAsync();
            var sqliteActivation = await sqlite.ActivateProviderAsync();
            Assert.IsTrue(!s3Activation.IsError && s3Activation.Result, s3Activation.Message);
            Assert.IsTrue(!sqliteActivation.IsError && sqliteActivation.Result, sqliteActivation.Message);

            var dna = new OASISDNA { OASIS = new NextGenSoftware.OASIS.API.DNA.OASIS
                { HyperDriveMode = HyperDriveModes.V2, StorageProviders = new StorageProviderSettings() } };
            dna.OASIS.SubscriptionConfig.PlanType = "Gold";
            var providerTypes = new[] { ProviderType.AWSS3OASIS, ProviderType.SQLLiteDBOASIS };
            var candidates = providerTypes.Select(type => new EnumValue<ProviderType>(type)).ToArray();
            var replication = new ProviderManager(null, dna) { IsAutoReplicationEnabled = true };
            replication.RegisterProvider(s3);
            replication.RegisterProvider(sqlite);
            Assert.IsFalse(replication.SetAndReplaceAutoReplicationListForProviders(candidates).IsError);
            var replicationRouter = new OASISHyperDrive(replication);
            var records = new List<Holon>();
            foreach (var primary in providerTypes)
            {
                var holon = new Holon { Id = Guid.NewGuid(), Name = $"priority routing {primary}", CreatedByAvatarId = Guid.NewGuid() };
                attempted.Add(holon.Id);
                var request = new StorageOperationRequest { Operation = "SaveHolon", PreferredProvider = primary, Payload = holon, SaveChildren = false };
                var saved = sync ? replicationRouter.RouteRequest<IHolon>(request) : await replicationRouter.RouteRequestAsync<IHolon>(request);
                Assert.IsFalse(saved.IsError || saved.IsWarning, saved.Message);
                foreach (var provider in new IOASISStorageProvider[] { s3, sqlite })
                {
                    var direct = await provider.LoadHolonAsync(holon.Id, loadChildren: false);
                    Assert.IsFalse(direct.IsError, direct.Message);
                    AssertRecord(holon, direct.Result);
                }
                records.Add(holon);
            }

            var balancing = new ProviderManager(null, dna) { IsAutoLoadBalanceEnabled = true };
            balancing.RegisterProvider(s3);
            balancing.RegisterProvider(sqlite);
            Assert.IsFalse(balancing.SetAndReplaceAutoLoadBalanceListForProviders(candidates).IsError);
            var balancedRouter = new OASISHyperDrive(balancing);
            var selected = records[0];
            var served = new List<string>();
            for (var index = 0; index < 6; index++)
            {
                var read = new StorageOperationRequest { Operation = "LoadHolon", HolonId = selected.Id, LoadChildren = false };
                var result = sync ? balancedRouter.RouteRequest<IHolon>(read, LoadBalancingStrategy.RoundRobin)
                    : await balancedRouter.RouteRequestAsync<IHolon>(read, LoadBalancingStrategy.RoundRobin);
                Assert.IsFalse(result.IsError, result.Message);
                AssertRecord(selected, result.Result);
                using var route = JsonDocument.Parse(result.MetaData["hyperDriveRoute"]);
                var initial = route.RootElement.GetProperty("InitialProvider").GetString();
                Assert.AreEqual(initial, route.RootElement.GetProperty("ServingProvider").GetString());
                Assert.AreEqual("RoundRobin", route.RootElement.GetProperty("RequestedStrategy").GetString());
                served.Add(initial!);
            }
            foreach (var provider in providerTypes)
                Assert.AreEqual(3, served.Count(value => value == provider.ToString()));

            using var unavailableEndpoint = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            unavailableEndpoint.Bind(new IPEndPoint(IPAddress.Loopback, 0));
            var unavailableUrl = $"http://127.0.0.1:{((IPEndPoint)unavailableEndpoint.LocalEndPoint!).Port}";
            using var unavailableClient = NativeClient(unavailableUrl);
            using var unavailableS3 = new AwsS3Provider(unavailableClient, bucket, () => key, "routing/");
            await VerifyFailoverAsync(dna, candidates, selected, sync, unavailableS3, sqlite);

            var unavailablePath = Path.Combine(fixture, "missing.db");
            using var unavailableSqlite = new SqliteProvider($"Data Source={unavailablePath};Mode=ReadWrite;Pooling=False;Default Timeout=1");
            await VerifyFailoverAsync(dna, candidates, selected, sync, unavailableSqlite, s3);
            Assert.IsFalse(File.Exists(unavailablePath));
        }
        finally
        {
            try
            {
                foreach (var id in attempted)
                {
                    var record = await s3.LoadHolonAsync(id, loadChildren: false);
                    if (record.Result != null)
                    {
                        var deleted = await s3.DeleteHolonAsync(id);
                        Assert.IsFalse(deleted.IsError, deleted.Message);
                    }
                }
                var after = await client.ListObjectsV2Async(new ListObjectsV2Request { BucketName = bucket, MaxKeys = 1 });
                Assert.AreEqual(0, after.S3Objects.Count, "The native bucket must be empty after exact cleanup.");
            }
            finally
            {
                await s3.DeActivateProviderAsync();
            }
            await sqlite.DeActivateProviderAsync();
        }
    }

    private static async Task VerifyFailoverAsync(OASISDNA dna, EnumValue<ProviderType>[] candidates,
        Holon expected, bool sync, IOASISStorageProvider unavailable, IOASISStorageProvider replica)
    {
        var failover = new ProviderManager(null, dna) { IsAutoFailOverEnabled = false };
        failover.RegisterProvider(unavailable);
        failover.RegisterProvider(replica);
        Assert.IsFalse(failover.SetAndReplaceAutoFailOverListForProviders(candidates).IsError);
        var router = new OASISHyperDrive(failover);
        var source = unavailable.ProviderType.Value;
        StorageOperationRequest ReadSource() => new() { Operation = "LoadHolon", HolonId = expected.Id,
            LoadChildren = false, PreferredProvider = source };

        var rejected = sync ? router.RouteRequest<IHolon>(ReadSource()) : await router.RouteRequestAsync<IHolon>(ReadSource());
        Assert.IsTrue(rejected.IsError, rejected.Message);
        failover.IsAutoFailOverEnabled = true;
        var recovered = sync ? router.RouteRequest<IHolon>(ReadSource()) : await router.RouteRequestAsync<IHolon>(ReadSource());
        Assert.IsFalse(recovered.IsError, recovered.Message);
        AssertRecord(expected, recovered.Result);
        using var route = JsonDocument.Parse(recovered.MetaData["hyperDriveRoute"]);
        Assert.AreEqual(source.ToString(), route.RootElement.GetProperty("RequestedProvider").GetString());
        Assert.AreEqual(replica.ProviderType.Value.ToString(), route.RootElement.GetProperty("ServingProvider").GetString());
    }

    private static void AssertRecord(Holon expected, IHolon? actual)
    {
        Assert.IsNotNull(actual);
        Assert.AreEqual(expected.Id, actual.Id);
        Assert.AreEqual(expected.Name, actual.Name);
        Assert.AreEqual(expected.CreatedByAvatarId, actual.CreatedByAvatarId);
    }
}
