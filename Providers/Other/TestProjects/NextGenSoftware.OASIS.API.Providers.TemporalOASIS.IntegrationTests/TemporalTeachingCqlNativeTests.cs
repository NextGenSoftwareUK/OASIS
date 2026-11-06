using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.Common;
using Temporalio.Client;

namespace NextGenSoftware.OASIS.API.Providers.TemporalOASIS.IntegrationTests;

[TestClass]
public sealed class TemporalTeachingCqlNativeTests
{
    private const string Namespace = "oasis-cql";
    private const ProviderType Provider = ProviderType.TemporalOASIS;
    private static readonly Guid Owner = Guid.Parse("a5808c3e-7031-47e7-92de-48a659568f1b");

    [TestMethod, TestCategory("TemporalTeachingCqlNative")]
    public async Task OfficialServerProvidesThreeTwoAndZeroExactRunDescriptions()
    {
        if (Environment.GetEnvironmentVariable("COSMIC_TEMPORAL_VERIFY_PUBLIC") != "owned-linux-7233")
        {
            Assert.Inconclusive("Enable the explicitly owned Temporal Linux fixture.");
            return;
        }

        var client = await TemporalClient.ConnectAsync(new TemporalClientConnectOptions("127.0.0.1:7233") { Namespace = Namespace });
        var prefix = "oasis-teaching-" + Guid.NewGuid().ToString("N") + "-";
        var runs = new List<TemporalTeachingRun>();
        try
        {
            for (var i = 0; i < 3; i++)
            {
                var workflowId = prefix + i;
                var handle = await client.StartWorkflowAsync("OasisCqlTeaching", Array.Empty<object>(),
                    new WorkflowOptions(workflowId, "oasis-cql-teaching")
                    {
                        Rpc = new RpcOptions { Retry = false, Timeout = TimeSpan.FromSeconds(10) }
                    });
                runs.Add(new TemporalTeachingRun(workflowId, Guid.ParseExact(handle.ResultRunId, "D")));
            }

            var missing = new TemporalTeachingRun(runs[0].WorkflowId, Guid.NewGuid());
            var source = new TemporalTeachingQuerySource(client, Owner, runs.Append(missing), runs[0]);
            var activation = await source.ActivateQuerySourceAsync();
            Assert.IsFalse(activation.IsError, activation.Exception?.Message);
            var ids = runs.Select(run => TemporalTeachingQuerySource.LogicalId(Owner, Namespace, run)).ToArray();
            var missingId = TemporalTeachingQuerySource.LogicalId(Owner, Namespace, missing);
            Check(await Execute(source, ids.Append(missingId).ToArray()), ids);
            Check(await Execute(source, ids[..2]), ids[..2]);
            Check(await Execute(source, new[] { missingId }), Array.Empty<Guid>());
            var foreign = await Execute(source, new[] { Guid.NewGuid() });
            Assert.IsTrue(foreign.IsError);
            Assert.IsNull(foreign.Result);
        }
        finally
        {
            await Task.WhenAll(runs.Select(run => client.GetWorkflowHandle(run.WorkflowId, run.RunId.ToString("D"))
                .TerminateAsync("Owned CQL qualification finished",
                    new WorkflowTerminateOptions { Rpc = new RpcOptions { Retry = false, Timeout = TimeSpan.FromSeconds(10) } })));
        }
    }

    private static Task<OASISResult<IReadOnlyList<IHolon>>> Execute(TemporalTeachingQuerySource source, Guid[] selected)
    {
        var query = CosmicQuery<Holon>.Create(CosmicQueryScope.Named(Provider).Result)
            .WithReadAccess(CosmicQueryReadAccess.AuthoritativeRecords(Owner, Provider, selected).Result).Result
            .Where(row => selected.Contains(row.Id)).Result;
        var plan = CosmicQueryPlanner.Plan(query, Provider, new[] { new CosmicQueryTarget(Provider, source.QueryCapabilities) });
        Assert.IsFalse(plan.IsError, plan.Message);
        return source.ExecuteCosmicQueryAsync(plan.Result);
    }

    private static void Check(OASISResult<IReadOnlyList<IHolon>> result, Guid[] expected)
    {
        Assert.IsFalse(result.IsError, result.Exception?.Message);
        CollectionAssert.AreEquivalent(expected, result.Result.Select(row => row.Id).ToArray());
        foreach (var row in result.Result)
        {
            Assert.IsTrue(row.IsPublic && row.CreatedByAvatarId == Owner);
            Assert.AreEqual("Running", row.MetaData["TemporalStatus"].ToString());
            Assert.AreEqual(Namespace, row.MetaData["TemporalNamespace"].ToString());
            Assert.IsTrue(Guid.TryParseExact(row.MetaData["TemporalRunId"].ToString(), "D", out _));
            Assert.IsFalse(row.MetaData.ContainsKey("Memo"));
        }
    }
}
