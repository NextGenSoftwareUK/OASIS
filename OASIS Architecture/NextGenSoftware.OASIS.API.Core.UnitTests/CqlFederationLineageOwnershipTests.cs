using System.Text.Json;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc;
using Moq;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.API.Core.Services.Subscriptions;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Cql;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Core.UnitTests;

public partial class CqlV1Web4Tests
{
    [Theory]
    [InlineData(false, "remove")]
    [InlineData(false, "replace")]
    [InlineData(false, "wrong-type")]
    [InlineData(true, "remove")]
    [InlineData(true, "replace")]
    [InlineData(true, "wrong-type")]
    public async Task CursorLineageRetainsAdmittedRootAcrossContextMutation(bool resumed, string mutation)
    {
        var host = new CqlFederationCursorHost(new EphemeralDataProtectionProvider(), new string('2', 64));
        var sources = CursorSources();
        var root = CursorAdmission(host, Guid.NewGuid());
        var expected = Guid.Parse(((UsageExecutionHandle)root.Http.Items[Web4UsageMeasurement.ExecutionKey]!).OperationId);
        string? token = null;
        var admitted = root;
        if (resumed)
        {
            var first = Assert.IsType<FileContentResult>(await root.Policy.RunAsync(() =>
                CqlV1Endpoint.ExecuteFederationAsync(root.Http, CursorRequest(), sources)));
            using var json = JsonDocument.Parse(first.FileContents);
            var next = json.RootElement.GetProperty("result").GetProperty("continuation");
            token = next.GetProperty("cursor").GetString();
            admitted = CursorAdmission(host, Guid.Parse(next.GetProperty("nextRequestKey").GetString()!));
            sources = CursorSources();
        }
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var native = new TaskCompletionSource<OASISResult<IReadOnlyList<IHolon>>>(TaskCreationOptions.RunContinuationsAsynchronously);
        sources.Items[ProviderType.MongoDBOASIS].Adapter.Setup(value => value.ExecuteCosmicQueryAsync(
                It.IsAny<CosmicQueryPlan<Holon>>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .Returns(() => { entered.SetResult(); return native.Task; });
        var run = admitted.Policy.RunAsync(() => CqlV1Endpoint.ExecuteFederationAsync(admitted.Http, CursorRequest(token), sources));
        await entered.Task.WaitAsync(TimeSpan.FromSeconds(5));
        if (mutation == "remove") admitted.Http.Items.Remove(Web4UsageMeasurement.ExecutionKey);
        else admitted.Http.Items[Web4UsageMeasurement.ExecutionKey] = mutation == "wrong-type" ? "not-an-admission"
            : CursorAdmission(host, Guid.NewGuid()).Http.Items[Web4UsageMeasurement.ExecutionKey];
        native.SetResult(new(sources.Items[ProviderType.MongoDBOASIS].Rows));
        var response = Assert.IsType<FileContentResult>(await run);
        using var output = JsonDocument.Parse(response.FileContents);
        var opaque = output.RootElement.GetProperty("result").GetProperty("continuation").GetProperty("cursor").GetString()!;
        var state = host.Read(opaque, Actor);
        Assert.False(state.IsError, state.Message);
        Assert.Equal(expected, state.Result.Lineage);
        Assert.Equal(3, admitted.Receipts.Count);
    }
}
