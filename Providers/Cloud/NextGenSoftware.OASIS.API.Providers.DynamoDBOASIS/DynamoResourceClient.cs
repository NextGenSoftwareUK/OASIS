using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Amazon.Runtime;
using Amazon.Runtime.Internal;
using NextGenSoftware.OASIS.API.Core.Services.Subscriptions;

namespace NextGenSoftware.OASIS.API.Providers.DynamoDBOASIS;

// AWS SDK pipeline only; pricing, admission and durable receipt accounting belong to Core.
internal sealed class DynamoResourceClient : AmazonDynamoDBClient
{
    private readonly DynamoResourceCapture capture;
    internal DynamoResourceClient(AWSCredentials credentials, AmazonDynamoDBConfig config, DynamoResourceCapture capture) : base(credentials, config) => this.capture = capture;
    internal DynamoResourceClient(AmazonDynamoDBConfig config, DynamoResourceCapture capture) : base(config) => this.capture = capture;
    protected override void CustomizeRuntimePipeline(RuntimePipeline pipeline)
    {
        base.CustomizeRuntimePipeline(pipeline);
        pipeline.AddHandlerBefore<Marshaller>(new CapacityHandler(() => capture));
    }

    private sealed class CapacityHandler(Func<DynamoResourceCapture> owner) : PipelineHandler
    {
        public override async Task<T> InvokeAsync<T>(IExecutionContext context)
        {
            var invocation = owner().Begin(context.RequestContext.OriginalRequest);
            T response = default;
            Exception failure = null;
            try { response = await base.InvokeAsync<T>(context).ConfigureAwait(false); }
            catch (Exception cause) { failure = cause; }
            try { if (invocation != null) await invocation.ObserveAsync(response, failure).ConfigureAwait(false); }
            catch (Exception observationFailure)
            {
                // Metering/persistence loss is not an AWS outage. The existing HyperDrive
                // evidence-integrity fence must stop failover rather than repeat a paid call.
                var terminal = new System.IO.InvalidDataException("Native DynamoDB resource observation could not be retained; do not repeat the request.", observationFailure);
                if (failure != null) throw new AggregateException("Native DynamoDB request and resource observation both failed.", failure, terminal);
                throw terminal;
            }
            if (failure != null) ExceptionDispatchInfo.Capture(failure).Throw();
            return response;
        }
    }
}

internal sealed class DynamoResourceCapture(Func<string> provider, string region)
{
    internal sealed class Scope(string requestId, Func<string, IReadOnlyList<NativeResourceUsageObservation>, Task> observer)
    {
        internal int Active = 1;
        internal readonly string RequestId = requestId;
        internal readonly Func<string, IReadOnlyList<NativeResourceUsageObservation>, Task> Observer = observer;
        internal void RequireActive()
        {
            if (Volatile.Read(ref Active) == 0) throw new InvalidOperationException("DynamoDB resource observation scope has ended; detached work needs its own admission.");
        }
    }
    private readonly AsyncLocal<Scope> current = new();
    internal async Task<T> RunAsync<T>(string requestId, Func<Task<T>> operation, Func<string, IReadOnlyList<NativeResourceUsageObservation>, Task> observer)
    {
        ArgumentNullException.ThrowIfNull(operation); ArgumentNullException.ThrowIfNull(observer);
        // Reuse Core's exact UTF-8 identity contract before SDK I/O, not only after a paid response.
        // This validation-only incomplete object is never emitted or persisted as usage evidence.
        var owner = provider();
        new NativeResourceUsageObservation(owner, requestId, null, "admission", "capacity-units", "Total", null, DateTime.UtcNow)
            .Validate(owner, requestId);
        if (current.Value != null) throw new InvalidOperationException("DynamoDB resource observation ownership cannot be nested or replaced.");
        var scope = new Scope(requestId, observer); current.Value = scope;
        try { return await operation().ConfigureAwait(false); }
        finally { Interlocked.Exchange(ref scope.Active, 0); current.Value = null; }
    }

    internal Invocation Begin(AmazonWebServiceRequest request)
    {
        var scope = current.Value; scope?.RequireActive();
        string[] tables; string operation;
        switch (request)
        {
            case GetItemRequest value: operation = "GetItem"; value.ReturnConsumedCapacity = ReturnConsumedCapacity.INDEXES; tables = new[] { value.TableName }; break;
            case PutItemRequest value: operation = "PutItem"; value.ReturnConsumedCapacity = ReturnConsumedCapacity.INDEXES; tables = new[] { value.TableName }; break;
            case ScanRequest value: operation = "Scan"; value.ReturnConsumedCapacity = ReturnConsumedCapacity.INDEXES; tables = new[] { value.TableName }; break;
            case UpdateItemRequest value: operation = "UpdateItem"; value.ReturnConsumedCapacity = ReturnConsumedCapacity.INDEXES; tables = new[] { value.TableName }; break;
            case DeleteItemRequest value: operation = "DeleteItem"; value.ReturnConsumedCapacity = ReturnConsumedCapacity.INDEXES; tables = new[] { value.TableName }; break;
            case TransactWriteItemsRequest value:
                operation = "TransactWriteItems";
                value.ReturnConsumedCapacity = ReturnConsumedCapacity.INDEXES;
                tables = value.TransactItems.Select(item => item.Put?.TableName ?? item.Update?.TableName ?? item.Delete?.TableName ?? item.ConditionCheck?.TableName).ToArray(); break;
            default: return null; // Describe/Create control-plane requests do not expose capacity.
        }
        return scope == null ? null : new Invocation(scope, provider(), region, tables, request is TransactWriteItemsRequest, operation);
    }

    internal sealed class Invocation
    {
        private readonly Scope scope;
        private readonly string provider, region;
        private readonly string[] tables;
        private readonly bool transaction;
        private readonly string operation;
        internal Invocation(Scope scope, string provider, string region, string[] tables, bool transaction, string operation)
        { this.scope = scope; this.provider = provider; this.region = region; this.tables = tables; this.transaction = transaction; this.operation = operation; }
        internal async Task ObserveAsync(AmazonWebServiceResponse response, Exception failure)
        {
            scope.RequireActive();
            var nativeId = response?.ResponseMetadata?.RequestId ?? (failure as AmazonServiceException)?.RequestId;
            IReadOnlyList<NativeResourceUsageObservation> rows;
            if (transaction) rows = DynamoCapacityUsage.CaptureTransaction(provider, scope.RequestId, nativeId, region, tables,
                (response as TransactWriteItemsResponse)?.ConsumedCapacity, DateTime.UtcNow);
            else
            {
                var capacity = response switch { GetItemResponse value => value.ConsumedCapacity, PutItemResponse value => value.ConsumedCapacity, ScanResponse value => value.ConsumedCapacity,
                    UpdateItemResponse value => value.ConsumedCapacity, DeleteItemResponse value => value.ConsumedCapacity, _ => null };
                rows = DynamoCapacityUsage.Capture(provider, scope.RequestId, nativeId, region, tables[0], capacity, DateTime.UtcNow);
            }
            await scope.Observer(operation, rows).ConfigureAwait(false);
        }
    }
}
