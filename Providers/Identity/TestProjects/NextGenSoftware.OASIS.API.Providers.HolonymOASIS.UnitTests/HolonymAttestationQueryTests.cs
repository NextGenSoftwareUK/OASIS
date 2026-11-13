using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Numerics;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Nethereum.ABI;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.HolonymOASIS.UnitTests;

[TestClass]
public class HolonymAttestationQueryTests
{
    private const ProviderType Provider = ProviderType.HolonymOASIS;
    private static readonly Guid Owner = Guid.Parse("843da756-18b2-4b28-bb41-57d1051c6083");
    private static readonly string[] Keys = HolonymAttestationQuerySource.ReviewedIds.ToArray();
    private static readonly Guid[] Ids = Keys.Select(key => HolonymAttestationQuerySource.LogicalId(Owner, key)).ToArray();

    [TestMethod]
    public async Task ExactNativeRecordsPreserveLiteralCircuitAndAllUnsignedValues()
    {
        using var replies = new Replies();
        using var source = new HolonymAttestationQuerySource(Owner, Keys, replies);
        var activation = await source.ActivateQuerySourceAsync();
        Assert.IsFalse(activation.IsError, activation.Exception?.ToString());
        var result = await Read(source, Ids); Check(result, Ids);
        foreach (var row in result.Result)
        {
            Assert.AreEqual(Replies.Circuit, row.MetaData["HolonymCircuitIdRaw"].ToString());
            StringAssert.Contains(row.MetaData["HolonymPublicValuesJson"].ToString(), ((BigInteger.One << 256) - 1).ToString());
            Assert.IsFalse(row.MetaData.ContainsKey("syncAt"));
        }
        Check(await Read(source, Ids.Take(2).ToArray()), Ids.Take(2).ToArray());
        Assert.AreEqual(6, replies.Requests);
        var mixed = await Read(source, new[] { Ids[0], Guid.NewGuid() });
        Assert.IsTrue(mixed.IsError); Assert.IsNull(mixed.Result); Assert.AreEqual(6, replies.Requests);
    }

    [DataTestMethod]
    [DataRow("wrong-id")][DataRow("wrong-schema")][DataRow("wrong-attester")]
    [DataRow("wrong-chain")][DataRow("wrong-mode")][DataRow("wrong-location")]
    [DataRow("wrong-types")][DataRow("error-envelope")][DataRow("duplicate-field")]
    [DataRow("oversized")][DataRow("wrong-content-type")][DataRow("abi-offset")]
    [DataRow("abi-array-bomb")][DataRow("abi-padding")][DataRow("abi-trailing")]
    [DataRow("revoked-without-time")][DataRow("negative-time")][DataRow("wrong-transaction")]
    public async Task UnverifiedBindingsAndUnsafeAbiNeverActivate(string fault)
    {
        using var replies = new Replies { Fault = fault };
        using var source = new HolonymAttestationQuerySource(Owner, Keys, replies);
        var result = await source.ActivateQuerySourceAsync();
        Assert.IsTrue(result.IsError, fault); Assert.IsFalse(source.IsProviderActivated);
        Assert.AreEqual(1, replies.Requests, "No alternate route or retry after a failed native read.");
    }

    [TestMethod]
    public async Task OneFailedRecordPublishesNoPartialRows()
    {
        using var replies = new Replies();
        using var source = new HolonymAttestationQuerySource(Owner, Keys, replies);
        var activation = await source.ActivateQuerySourceAsync();
        Assert.IsFalse(activation.IsError, activation.Exception?.ToString());
        replies.FailId = Keys[1]; var result = await Read(source, Ids);
        Assert.IsTrue(result.IsError); Assert.IsNull(result.Result); Assert.AreEqual(4, replies.Requests);
    }

    [TestMethod]
    public async Task MovingIndexMaintenanceDoesNotChangeAttestationAttributes()
    {
        using var replies = new Replies();
        using var source = new HolonymAttestationQuerySource(Owner, Keys, replies);
        var activation = await source.ActivateQuerySourceAsync();
        Assert.IsFalse(activation.IsError, activation.Exception?.ToString());
        var before = await Read(source, Ids.Take(1).ToArray()); replies.Sync++;
        var after = await Read(source, Ids.Take(1).ToArray());
        Assert.IsFalse(before.IsError || after.IsError);
        foreach (var key in before.Result[0].MetaData.Keys)
            Assert.AreEqual(before.Result[0].MetaData[key]?.ToString(), after.Result[0].MetaData[key]?.ToString());
    }

    [TestMethod]
    public async Task InactiveAndCancelledReadsDoNotDispatch()
    {
        using var replies = new Replies();
        using var source = new HolonymAttestationQuerySource(Owner, Keys, replies);
        Assert.IsTrue((await Read(source, Ids)).IsError);
        using var cancellation = new CancellationTokenSource(); cancellation.Cancel();
        Assert.IsTrue((await source.ActivateQuerySourceAsync(cancellation.Token)).IsError);
        Assert.AreEqual(0, replies.Requests);
    }

    [TestMethod]
    public void InvalidCataloguesFailWithoutConstructingTransport()
    {
        foreach (var keys in new[] { Array.Empty<string>(), new[] { Keys[0], Keys[0] }, new[] { "../foreign" }, new[] { "onchain_evm_8453_0x1" }, new[] { "onchain_evm_10_0x01" }, Enumerable.Range(1, 9).Select(x => "onchain_evm_10_0x" + x.ToString("x")).ToArray() })
            Assert.ThrowsException<ArgumentException>(() => new HolonymAttestationQuerySource(Owner, keys));
        Assert.ThrowsException<ArgumentException>(() => new HolonymAttestationQuerySource(Guid.Empty, Keys));
    }

    [TestMethod, TestCategory("HolonymAttestationNative")]
    public async Task RealCurrentAttestationCqlReturnsThreeAndTwoExactRecords()
    {
        if (Environment.GetEnvironmentVariable("COSMIC_HOLONYM_VERIFY_PUBLIC") != "1")
        { Assert.Inconclusive("Explicitly opt in to this read-only native schedule."); return; }
        using var source = HolonymAttestationQuerySource.ForReviewedTeachingCatalogue(Owner);
        var activation = await source.ActivateQuerySourceAsync();
        Assert.IsFalse(activation.IsError, activation.Exception?.ToString());
        Check(await Read(source, Ids), Ids);
        Check(await Read(source, Ids.Take(2).ToArray()), Ids.Take(2).ToArray());
    }

    private static Task<OASISResult<IReadOnlyList<IHolon>>> Read(HolonymAttestationQuerySource source, Guid[] ids)
    {
        var query = CosmicQuery<Holon>.Create(CosmicQueryScope.Named(Provider).Result)
            .WithReadAccess(CosmicQueryReadAccess.AuthoritativeRecords(Owner, Provider, ids).Result).Result
            .Where(row => ids.Contains(row.Id)).Result;
        var plan = CosmicQueryPlanner.Plan(query, Provider, new[] { new CosmicQueryTarget(Provider, source.QueryCapabilities) });
        Assert.IsFalse(plan.IsError, plan.Message); return source.ExecuteCosmicQueryAsync(plan.Result);
    }
    private static void Check(OASISResult<IReadOnlyList<IHolon>> result, Guid[] expected)
    {
        Assert.IsFalse(result.IsError, result.Exception?.ToString());
        CollectionAssert.AreEquivalent(expected, result.Result.Select(row => row.Id).ToArray());
        foreach (var row in result.Result)
        {
            Assert.IsTrue(row.IsPublic && row.IsActive && row.CreatedByAvatarId == Owner && !row.Children.Any());
            Assert.AreEqual(HolonymAttestationQuerySource.SchemaId, row.MetaData["HolonymSchemaId"].ToString());
            Assert.IsTrue(Keys.Contains(row.MetaData["HolonymNativeId"].ToString()));
        }
    }

    private sealed class Replies : HttpMessageHandler
    {
        public const string Circuit = "0xb5121226395e3b6c76eb8ddfb0bf2f2075e7f2c6956567e84b38a223c3a3d15";
        public string Fault { get; init; }
        public string FailId { get; set; }
        public int Sync = 1;
        public int Requests;
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
        {
            Interlocked.Increment(ref Requests);
            Assert.AreEqual(HttpMethod.Get, request.Method); Assert.IsNull(request.Headers.Authorization);
            Assert.AreEqual("https://mainnet-rpc.sign.global", request.RequestUri.GetLeftPart(UriPartial.Authority));
            Assert.IsTrue(request.RequestUri.AbsolutePath.StartsWith("/api/index/attestations/", StringComparison.Ordinal));
            var key = request.RequestUri.Segments.Last(); Assert.IsTrue(Keys.Contains(key));
            if (key == FailId) return Task.FromResult(new HttpResponseMessage(HttpStatusCode.ServiceUnavailable));
            var encoded = new ABIEncode().GetABIEncoded(new ABIValue("string", Circuit),
                new ABIValue("uint256[]", new BigInteger[] { 1, 2, 3, 4, (BigInteger.One << 256) - 1 }));
            if (Fault == "abi-offset") encoded[31] = 96;
            if (Fault == "abi-array-bomb") Array.Fill(encoded, (byte)255, 192, 32);
            if (Fault == "abi-padding") encoded[161] = 1;
            if (Fault == "abi-trailing") encoded = encoded.Concat(new byte[32]).ToArray();
            var schema = new { id = HolonymAttestationQuerySource.SchemaId, schemaId = "0x1", mode = "onchain", chainType = "evm", chainId = "10", dataLocation = "onchain",
                data = new[] { new { name = "circuitId", type = "string" }, new { name = "publicValues", type = "uint256[]" } } };
            var body = JsonSerializer.Serialize(new { success = true, statusCode = 200, data = new {
                id = key, attestationId = key[15..], fullSchemaId = HolonymAttestationQuerySource.SchemaId, schemaId = "0x1", attester = HolonymAttestationQuerySource.Attester,
                mode = "onchain", chainType = "evm", chainId = "10", dataLocation = "onchain", schema,
                data = "0x" + Convert.ToHexString(encoded).ToLowerInvariant(), transactionHash = "0x" + new string('a', 64),
                attestTimestamp = "1700000000000", validUntil = "1800000000", revoked = false, revokeTimestamp = (string)null,
                recipients = new[] { "0x" + new string('1', 40) }, syncAt = Sync, extra = new { sync = Sync } } });
            var parsed = JsonNode.Parse(body); var row = parsed["data"];
            switch (Fault)
            {
                case "wrong-id": row["id"] = Keys.Last(); break;
                case "wrong-schema": row["fullSchemaId"] = "onchain_evm_10_0x2"; break;
                case "wrong-attester": row["attester"] = "0x" + new string('1', 40); break;
                case "wrong-chain": row["chainId"] = "8453"; break;
                case "wrong-mode": row["mode"] = "offchain"; break;
                case "wrong-location": row["dataLocation"] = "ipfs"; break;
                case "wrong-types": row["schema"]["data"][1]["type"] = "int256[]"; break;
                case "error-envelope": parsed["success"] = false; break;
                case "revoked-without-time": row["revoked"] = true; break;
                case "negative-time": row["validUntil"] = "-1"; break;
                case "wrong-transaction": row["transactionHash"] = "0x00"; break;
            }
            body = parsed.ToJsonString();
            if (Fault == "duplicate-field") body = body.Replace("\"success\":true", "\"success\":true,\"success\":true");
            if (Fault == "oversized") body += new string(' ', 16385);
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(body, Encoding.UTF8, Fault == "wrong-content-type" ? "text/html" : "application/json") });
        }
    }
}
