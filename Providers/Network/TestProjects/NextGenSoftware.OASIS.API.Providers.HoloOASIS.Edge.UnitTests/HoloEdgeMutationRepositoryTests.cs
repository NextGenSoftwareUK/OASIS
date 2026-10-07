using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using NextGenSoftware.OASIS.API.Core.Managers.OASISHyperDrive.Synchronization;
using NextGenSoftware.OASIS.API.Providers.HoloOASIS.Edge;
using NextGenSoftware.OASIS.Common;
using Xunit;

namespace NextGenSoftware.OASIS.API.Providers.HoloOASIS.Edge.UnitTests;

public sealed class HoloEdgeMutationRepositoryTests
{
    [Fact]
    public async Task ApplyLocalMutation_MapsExactHappWireContract()
    {
        var client = new RecordingClient();
        var repository = new HoloEdgeMutationRepository(client);
        var operation = NewOperation(SyncOperationKind.Upsert, "{\"name\":\"tree\"}");

        var result = await repository.ApplyLocalMutationAsync(operation, default);

        result.IsError.Should().BeFalse();
        client.Zome.Should().Be("oasis");
        client.Function.Should().Be("apply_hyperdrive_mutation");
        client.Payload!["operation_id"].Should().Be(operation.OperationId.ToString("D"));
        client.Payload["avatar_id"].Should().Be(operation.AvatarId.ToString("D"));
        client.Payload["entity_id"].Should().Be(operation.EntityId.ToString("D"));
        client.Payload["entity_type"].Should().Be(operation.EntityType);
        client.Payload["kind"].Should().Be(0);
        client.Payload["version_id"].Should().Be(operation.VersionId.ToString("D"));
        client.Payload["payload_json"].Should().Be(operation.PayloadJson);
    }

    [Fact]
    public async Task Delete_SendsNullPayloadAndDeleteKind()
    {
        var client = new RecordingClient();
        var repository = new HoloEdgeMutationRepository(client);

        var result = await repository.ApplyLocalMutationAsync(
            NewOperation(SyncOperationKind.Delete, "must-not-leak"), default);

        result.IsError.Should().BeFalse();
        client.Payload!["kind"].Should().Be(1);
        client.Payload["payload_json"].Should().BeNull();
    }

    [Fact]
    public async Task Command_IsRejectedWithoutCallingHolochain()
    {
        var client = new RecordingClient();
        var repository = new HoloEdgeMutationRepository(client);

        var result = await repository.ApplyLocalMutationAsync(
            NewOperation(SyncOperationKind.Command, "{}"), default);

        result.IsError.Should().BeTrue();
        result.ErrorCode.Should().Be("HOLO_EDGE_COMMAND_NOT_REPLICABLE");
        client.CallCount.Should().Be(0);
    }

    [Fact]
    public async Task InvalidMutation_IsRejectedBeforeDispatch()
    {
        var client = new RecordingClient();
        var repository = new HoloEdgeMutationRepository(client);
        var operation = NewOperation(SyncOperationKind.Upsert, "{}");
        operation.OperationId = Guid.Empty;

        var result = await repository.ApplyLocalMutationAsync(operation, default);

        result.ErrorCode.Should().Be("HOLO_EDGE_MUTATION_INVALID");
        client.CallCount.Should().Be(0);
    }

    [Fact]
    public async Task ClientFailure_IsReturnedWithoutASecondDispatch()
    {
        var client = new RecordingClient { Result = Error("HC_UNAVAILABLE") };
        var repository = new HoloEdgeMutationRepository(client);

        var result = await repository.ApplyLocalMutationAsync(
            NewOperation(SyncOperationKind.Upsert, "{}"), default);

        result.ErrorCode.Should().Be("HC_UNAVAILABLE");
        client.CallCount.Should().Be(1);
    }

    private static SyncOperation NewOperation(SyncOperationKind kind, string payload) => new()
    {
        OperationId = Guid.NewGuid(), AvatarId = Guid.NewGuid(), DeviceId = Guid.NewGuid(),
        EntityId = Guid.NewGuid(), EntityType = "holon", VersionId = Guid.NewGuid(),
        Kind = kind, PayloadJson = payload, CreatedUtc = DateTime.UtcNow
    };

    private static OASISResult<bool> Error(string code) => new()
    {
        IsError = true, ErrorCount = 1, ErrorCode = code, Message = code
    };

    private sealed class RecordingClient : IHoloEdgeAppClient
    {
        public string? Zome { get; private set; }
        public string? Function { get; private set; }
        public IReadOnlyDictionary<string, object>? Payload { get; private set; }
        public int CallCount { get; private set; }
        public OASISResult<bool> Result { get; set; } = new(true) { IsSaved = true };

        public Task<OASISResult<bool>> CallAsync(string zome, string function,
            IReadOnlyDictionary<string, object> payload, CancellationToken cancellationToken)
        {
            CallCount++;
            Zome = zome;
            Function = function;
            Payload = payload;
            return Task.FromResult(Result);
        }

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}
