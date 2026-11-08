using System.Collections.Concurrent;
using System.Diagnostics.Tracing;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;
using Moq;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.API.Core.Services.Subscriptions;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Models.Data;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Cql;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.API.Providers.CloudinaryOASIS;
using NextGenSoftware.OASIS.API.Providers.GUNOASIS;
using NextGenSoftware.OASIS.API.Providers.OrbitDBOASIS;
using NextGenSoftware.OASIS.API.Providers.ENSOffchainOASIS;
using NextGenSoftware.OASIS.API.Providers.OracleObjectStorageOASIS;
using NextGenSoftware.OASIS.API.Providers.ProofOfHumanityOASIS;
using NextGenSoftware.OASIS.API.Providers.SafeOASIS;
using NextGenSoftware.OASIS.API.Providers.DiscordOASIS;
using NextGenSoftware.OASIS.API.Providers.LivepeerOASIS;
using NextGenSoftware.OASIS.API.Providers.PinataOASIS;
using NextGenSoftware.OASIS.API.Providers.PushProtocolOASIS;
using NextGenSoftware.OASIS.API.Providers.LayerZeroOASIS;
using NextGenSoftware.OASIS.API.Providers.DecentralandOASIS;
using NextGenSoftware.OASIS.API.Providers.WakuOASIS;
using NextGenSoftware.OASIS.API.Providers.TheGraphOASIS;
using NextGenSoftware.OASIS.API.Providers.ReclaimProtocolOASIS;
using NextGenSoftware.OASIS.API.Providers.WorldIDOASIS;
using MongoSource = NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.MongoDBOASIS;
using IoFile = System.IO.File;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.UnitTests;

public sealed class CqlReviewedPublicMongoHttpTests
{
    [ReviewedPublicMongoNativeTheory]
    [InlineData(ProviderType.CloudinaryOASIS)]
    [InlineData(ProviderType.OracleObjectStorageOASIS)]
    [InlineData(ProviderType.ProofOfHumanityOASIS)]
    public Task ReviewedPublicRowsAndPrivateMongoCopiesProduceThreeTwoFiveFourOverHttp(ProviderType provider) =>
        VerifyHttpAsync(provider);

    [ReviewedPublicMongoNativeFact("COSMIC_REVIEWED_PUBLIC_SIDECAR_TOKEN")]
    public Task GunNativeHttp() => VerifyHttpAsync(ProviderType.GUNOASIS);

    [ReviewedPublicMongoNativeFact("COSMIC_REVIEWED_PUBLIC_SIDECAR_TOKEN")]
    public Task OrbitDbNativeHttp() => VerifyHttpAsync(ProviderType.OrbitDBOASIS);

    [ReviewedPublicMongoNativeFact("COSMIC_REVIEWED_PUBLIC_SAFE_NATIVE")]
    public Task SafeNativeHttp() => VerifyHttpAsync(ProviderType.SafeOASIS);

    [ReviewedPublicMongoNativeFact("COSMIC_REVIEWED_NETWORK_NATIVE")]
    public Task DiscordNativeHttp() => VerifyHttpAsync(ProviderType.DiscordOASIS);

    [ReviewedPublicMongoNativeFact("COSMIC_REVIEWED_NETWORK_NATIVE")]
    public Task LivepeerNativeHttp() => VerifyHttpAsync(ProviderType.LivepeerOASIS);

    [ReviewedPublicMongoNativeFact("COSMIC_REVIEWED_NETWORK_NATIVE")]
    public Task PinataNativeHttp() => VerifyHttpAsync(ProviderType.PinataOASIS);

    [ReviewedPublicMongoNativeFact("COSMIC_REVIEWED_NETWORK_NATIVE")]
    public Task PushProtocolNativeHttp() => VerifyHttpAsync(ProviderType.PushProtocolOASIS);

    [ReviewedPublicMongoNativeFact("COSMIC_REVIEWED_NETWORK_NATIVE")]
    public Task LayerZeroV1NativeHttp() => VerifyHttpAsync(ProviderType.LayerZeroOASIS);

    [ReviewedPublicMongoNativeFact("COSMIC_REVIEWED_NETWORK_NATIVE")]
    public Task WorldIdProtocolNativeHttp() => VerifyHttpAsync(ProviderType.WorldIDOASIS);

    [ReviewedPublicMongoNativeFact("COSMIC_REVIEWED_NETWORK_NATIVE")]
    public Task ReclaimVerifierNativeHttp() => VerifyHttpAsync(ProviderType.ReclaimProtocolOASIS);

    [ReviewedPublicMongoNativeFact("COSMIC_REVIEWED_NETWORK_NATIVE")]
    public Task DecentralandSceneNativeHttp() => VerifyHttpAsync(ProviderType.DecentralandOASIS);

    [ReviewedPublicMongoNativeFact("COSMIC_REVIEWED_NETWORK_NATIVE")]
    public Task TheGraphStudioNativeHttp() => VerifyHttpAsync(ProviderType.TheGraphOASIS);

    [ReviewedPublicMongoNativeFact("COSMIC_WAKU_STORE_HASHES")]
    public Task WakuStoreNativeHttp() => VerifyHttpAsync(ProviderType.WakuOASIS);

    [ReviewedPublicMongoNativeFact("COSMIC_PINATA_NATIVE_READ_CONTINUATION")]
    public Task PinataNativeHttpContinuation()
    {
        Assert.Equal("owned-loopback", Environment.GetEnvironmentVariable("COSMIC_PINATA_NATIVE_READ_CONTINUATION"));
        return VerifyHttpAsync(ProviderType.PinataOASIS, resume: true);
    }

    [ReviewedPublicMongoNativeFact("CQL_ENS_OFFCHAIN_QUERY_API_KEY")]
    public Task EnsOffchainNativeHttpContinuation() => VerifyHttpAsync(ProviderType.ENSOffchainOASIS, resume: true);

    private static Task VerifyHttpAsync(ProviderType provider, bool resume = false) =>
        ReviewedPublicMongoHttpFixture.VerifyHttpAsync(provider, owner => CreateReviewedSource(provider, owner), resume);

    private static (IOASISStorageProvider Source, Guid[] Ids) CreateReviewedSource(ProviderType provider, Guid owner)
    {
        switch (provider)
        {
            case ProviderType.CloudinaryOASIS:
            {
                string[] keys = { "sample.jpg", "lady.jpg", "woman.jpg" };
                return (new CloudinaryPublicImageQuerySource(owner, keys, keys[0]),
                    keys.Select(key => CloudinaryPublicImageQuerySource.LogicalId(owner, key)).ToArray());
            }
            case ProviderType.OracleObjectStorageOASIS:
            {
                string[] keys = { "bds-livelabs/env.sh", "bds-livelabs/download-all-hdfs-data.sh", "bds-livelabs/download-all-objstore.sh" };
                return (new OracleObjectStoragePublicMetadataQuerySource(owner, keys, keys[0]),
                    keys.Select(key => OracleObjectStoragePublicMetadataQuerySource.LogicalId(owner, key)).ToArray());
            }
            case ProviderType.ProofOfHumanityOASIS:
            {
                string[] keys = { "0x16044e1063c08670f8653055a786b7cc2034d2b0", "0xa478095886659168e8812154fb0de39f103e74b2",
                    "0xc5e9ddebb09cd64dfacab4011a0d5cedaf7c9bdb" };
                return (new ProofOfHumanityRegistryQuerySource(owner, keys, keys[0]),
                    keys.Select(key => ProofOfHumanityRegistryQuerySource.LogicalId(owner, key)).ToArray());
            }
            case ProviderType.ENSOffchainOASIS:
            {
                string[] keys = { "vitalik.eth", "nick.eth", "luc.eth" };
                return (new EnsOffchainQuerySource(owner, keys, keys[0],
                    Environment.GetEnvironmentVariable("CQL_ENS_OFFCHAIN_QUERY_API_KEY")),
                    keys.Select(key => EnsOffchainQuerySource.LogicalId(owner, key)).ToArray());
            }
            case ProviderType.DiscordOASIS:
            {
                Assert.Equal("owned-loopback", Environment.GetEnvironmentVariable("COSMIC_REVIEWED_NETWORK_NATIVE"));
                string[] guilds = { "267624335836053506", "1235157165589794909", "212250894228652034" };
                return (DiscordPublicGuildQuerySource.ForReviewedTeachingCatalogue(owner),
                    guilds.Select(guild => DiscordPublicGuildQuerySource.LogicalId(owner, guild)).ToArray());
            }
            case ProviderType.LivepeerOASIS:
            {
                Assert.Equal("owned-loopback", Environment.GetEnvironmentVariable("COSMIC_REVIEWED_NETWORK_NATIVE"));
                string[] addresses = { "0x525419ff5707190389bfb5c87c375d710f5fcb0e", "0xac2e50c8f7ac0f82923a7df9d9903f6ec4741919",
                    "0x4f4758f7167b18e1f5b3c1a7575e3eb584894dbc" };
                return (LivepeerOrchestratorQuerySource.ForReviewedTeachingCatalogue(owner),
                    addresses.Select(address => LivepeerOrchestratorQuerySource.LogicalId(owner, address)).ToArray());
            }
            case ProviderType.PinataOASIS:
            {
                Assert.Equal("owned-loopback", Environment.GetEnvironmentVariable("COSMIC_REVIEWED_NETWORK_NATIVE"));
                string[] blocks = { "QmP4eZj61tDxCmtb6aqSwQsrxRQAN2gwxTEXkTed9TujHh",
                    "QmeBt6f2Dem8QnZuZ8PDRT1iuWpphmza3gP17en2ZoReYR", "Qmb733sded7TRm8U8bM4FHif89jUVKd8yQiurKCKgCLyQk" };
                return (new PinataPublicBlockQuerySource(owner),
                    blocks.Select(cid => PinataPublicBlockQuerySource.LogicalId(owner, cid)).ToArray());
            }
            case ProviderType.PushProtocolOASIS:
            {
                Assert.Equal("owned-loopback", Environment.GetEnvironmentVariable("COSMIC_REVIEWED_NETWORK_NATIVE"));
                string[] channels = { "0xb88460bb2696cab9d66013a05dff29a28330689d",
                    "0x90a48d5cf7343b08da12e067680b4c6dbfe551be", "0xdbc5936e4daae94f415c39d284f6a69c4d553f2f" };
                return (new PushProtocolPublicChannelQuerySource(owner),
                    channels.Select(channel => PushProtocolPublicChannelQuerySource.LogicalId(owner, channel)).ToArray());
            }
            case ProviderType.LayerZeroOASIS:
            {
                Assert.Equal("owned-loopback", Environment.GetEnvironmentVariable("COSMIC_REVIEWED_NETWORK_NATIVE"));
                string[] networks = { "ethereum", "polygon", "arbitrum" };
                return (new LayerZeroV1EndpointQuerySource(owner),
                    networks.Select(network => LayerZeroV1EndpointQuerySource.LogicalId(owner, network)).ToArray());
            }
            case ProviderType.WorldIDOASIS:
            {
                Assert.Equal("owned-loopback", Environment.GetEnvironmentVariable("COSMIC_REVIEWED_NETWORK_NATIVE"));
                string[] components = { "registry", "verifier", "issuer-registry" };
                return (new WorldIdProtocolQuerySource(owner),
                    components.Select(component => WorldIdProtocolQuerySource.LogicalId(owner, component)).ToArray());
            }
            case ProviderType.ReclaimProtocolOASIS:
            {
                Assert.Equal("owned-loopback", Environment.GetEnvironmentVariable("COSMIC_REVIEWED_NETWORK_NATIVE"));
                string[] networks = { "base", "arbitrum", "polygon" };
                return (new ReclaimVerifierQuerySource(owner),
                    networks.Select(network => ReclaimVerifierQuerySource.LogicalId(owner, network)).ToArray());
            }
            case ProviderType.DecentralandOASIS:
            {
                Assert.Equal("owned-loopback", Environment.GetEnvironmentVariable("COSMIC_REVIEWED_NETWORK_NATIVE"));
                string[] parcels = { "0,0", "-53,71", "-100,-50" };
                return (new DecentralandSceneQuerySource(owner),
                    parcels.Select(parcel => DecentralandSceneQuerySource.LogicalId(owner, parcel)).ToArray());
            }
            case ProviderType.TheGraphOASIS:
            {
                Assert.Equal("owned-loopback", Environment.GetEnvironmentVariable("COSMIC_REVIEWED_NETWORK_NATIVE"));
                string[] deployments = { "adexto-arbitrum", "adexto-base", "klerosboard-gnosis" };
                return (new TheGraphStudioStatusQuerySource(owner),
                    deployments.Select(key => TheGraphStudioStatusQuerySource.LogicalId(owner, key)).ToArray());
            }
            case ProviderType.WakuOASIS:
            {
                var hashes = Environment.GetEnvironmentVariable("COSMIC_WAKU_STORE_HASHES")!.Split(',');
                var descriptor = JsonSerializer.Serialize(new { sourceOwner = owner, messageHashes = hashes });
                return (WakuStoreQueryConfiguration.CreateSource(descriptor),
                    hashes.Select(hash => WakuStoreQuerySource.LogicalId(owner, hash)).ToArray());
            }
            case ProviderType.SafeOASIS:
            {
                Assert.Equal("owned-loopback", Environment.GetEnvironmentVariable("COSMIC_REVIEWED_PUBLIC_SAFE_NATIVE"));
                string[] networks = { "eth", "base", "arb1" };
                return (new SafeIndexingQuerySource(owner, networks, networks[0],
                    Environment.GetEnvironmentVariable("CQL_SAFE_INDEXING_QUERY_API_KEY")),
                    networks.Select(network => SafeIndexingQuerySource.LogicalId(owner, network)).ToArray());
            }
            case ProviderType.GUNOASIS:
            case ProviderType.OrbitDBOASIS:
            {
                var fixture = Environment.GetEnvironmentVariable("COSMIC_REVIEWED_PUBLIC_SIDECAR_FIXTURE");
                var token = Environment.GetEnvironmentVariable("COSMIC_REVIEWED_PUBLIC_SIDECAR_TOKEN");
                Assert.False(string.IsNullOrWhiteSpace(fixture) || string.IsNullOrWhiteSpace(token));
                using var descriptor = JsonDocument.Parse(IoFile.ReadAllBytes(fixture!));
                var ids = descriptor.RootElement.GetProperty("ids").EnumerateArray().Take(3).Select(value => value.GetGuid()).ToArray();
                Assert.Equal(3, ids.Distinct().Count());
                Assert.DoesNotContain(Guid.Empty, ids);
                return provider == ProviderType.GUNOASIS
                    ? (new GunQuerySource(new Uri("http://127.0.0.1:54870/"), token!, ids[0]), ids)
                    : (new OrbitDbQuerySource(new Uri("http://127.0.0.1:54869/"), token!, ids[0]), ids);
            }
            default:
                throw new ArgumentException("Use one reviewed public native fixture.", nameof(provider));
        }
    }
}
