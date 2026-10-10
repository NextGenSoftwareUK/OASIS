using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;
using Temporalio.Client;
using Temporalio.Exceptions;

namespace NextGenSoftware.OASIS.API.Providers.TemporalOASIS
{
    /// <summary>Snapshot of a workflow execution as reported by the Temporal server.</summary>
    public sealed class TemporalWorkflowInfo
    {
        public string WorkflowId { get; init; }
        public string RunId { get; init; }
        public string WorkflowType { get; init; }
        public string Status { get; init; }
        public string TaskQueue { get; init; }
        public DateTime StartTime { get; init; }
        public DateTime? CloseTime { get; init; }
    }

    /// <summary>
    /// Temporal workflow orchestration provider using the official Temporal .NET SDK. Temporal is a durable workflow
    /// engine, not a data store, so this is an <see cref="OASISProvider"/> exposing workflow operations against an
    /// existing Temporal cluster (self-hosted or Temporal Cloud): start, signal, query, describe, list, cancel,
    /// terminate and await results. Workflows run on Temporal workers registered on the task queue.
    /// </summary>
    public class TemporalOASIS : OASISProvider
    {
        private readonly TemporalClientConnectOptions _connectOptions;
        private readonly string _taskQueue;
        private readonly SemaphoreSlim _connectLock = new(1, 1);
        private ITemporalClient _client;

        /// <param name="host">Frontend address, e.g. localhost:7233 or {namespace}.{account}.tmprl.cloud:7233.</param>
        /// <param name="ns">Temporal namespace.</param>
        /// <param name="taskQueue">Default task queue for workflows started through this provider.</param>
        /// <param name="apiKey">Temporal Cloud API key; enables TLS when set.</param>
        public TemporalOASIS(string host = "localhost:7233", string ns = "default", string taskQueue = "oasis-queue", string apiKey = null)
        {
            if (string.IsNullOrWhiteSpace(host)) throw new ArgumentException("A Temporal frontend address is required.", nameof(host));
            if (string.IsNullOrWhiteSpace(taskQueue)) throw new ArgumentException("A task queue is required.", nameof(taskQueue));
            _connectOptions = new TemporalClientConnectOptions(host) { Namespace = ns };
            if (!string.IsNullOrWhiteSpace(apiKey))
            {
                _connectOptions.ApiKey = apiKey;
                _connectOptions.Tls = new TlsOptions();
            }
            _taskQueue = taskQueue;
            ProviderName = "TemporalOASIS";
            ProviderDescription = "Temporal workflow orchestration provider (official Temporal .NET SDK).";
            ProviderType = new EnumValue<ProviderType>(Core.Enums.ProviderType.TemporalOASIS);
            ProviderCategory = new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.Network);
        }

        /// <summary>Uses an already connected client (e.g. a test or shared client).</summary>
        public TemporalOASIS(ITemporalClient client, string taskQueue)
            : this(client?.Connection?.Options?.TargetHost ?? "injected", client?.Options?.Namespace ?? "default", taskQueue)
        {
            _client = client ?? throw new ArgumentNullException(nameof(client));
        }

        private async Task<ITemporalClient> ClientAsync()
        {
            if (_client != null) return _client;
            await _connectLock.WaitAsync();
            try { return _client ??= await TemporalClient.ConnectAsync(_connectOptions); }
            finally { _connectLock.Release(); }
        }

        private async Task<OASISResult<T>> RunAsync<T>(string operation, Func<ITemporalClient, Task<T>> action, string message = null)
        {
            var result = new OASISResult<T>();
            try
            {
                result.Result = await action(await ClientAsync());
                result.Message = message;
            }
            catch (TemporalException ex) { OASISErrorHandling.HandleError(ref result, $"TemporalOASIS: {operation} failed: {ex.Message}", ex); }
            catch (Exception ex) { OASISErrorHandling.HandleError(ref result, $"TemporalOASIS: {operation} failed: {ex.Message}", ex); }
            return result;
        }

        public override async Task<OASISResult<bool>> ActivateProviderAsync()
        {
            var result = await RunAsync("activation", async c => await c.Connection.CheckHealthAsync(), "TemporalOASIS activated.");
            if (!result.IsError && !result.Result)
                OASISErrorHandling.HandleError(ref result, "TemporalOASIS: the Temporal frontend reported unhealthy.");
            IsProviderActivated = !result.IsError;
            return result;
        }

        public override OASISResult<bool> ActivateProvider() => ActivateProviderAsync().GetAwaiter().GetResult();

        public override Task<OASISResult<bool>> DeActivateProviderAsync()
        {
            IsProviderActivated = false;
            return Task.FromResult(new OASISResult<bool>(true) { Message = "TemporalOASIS deactivated." });
        }

        public override OASISResult<bool> DeActivateProvider() => DeActivateProviderAsync().GetAwaiter().GetResult();

        /// <summary>Starts a workflow by type name; returns the run id.</summary>
        public Task<OASISResult<string>> StartWorkflowAsync(string workflowType, string workflowId, IReadOnlyCollection<object> args = null, string taskQueue = null)
            => RunAsync("start workflow", async c =>
            {
                var handle = await c.StartWorkflowAsync(workflowType, args ?? Array.Empty<object>(), new WorkflowOptions(workflowId, taskQueue ?? _taskQueue));
                return handle.ResultRunId;
            }, $"Workflow '{workflowId}' started.");

        public Task<OASISResult<bool>> SignalWorkflowAsync(string workflowId, string signalName, IReadOnlyCollection<object> args = null, string runId = null)
            => RunAsync("signal workflow", async c => { await c.GetWorkflowHandle(workflowId, runId).SignalAsync(signalName, args ?? Array.Empty<object>()); return true; },
                $"Signal '{signalName}' sent to '{workflowId}'.");

        public Task<OASISResult<TResult>> QueryWorkflowAsync<TResult>(string workflowId, string queryName, IReadOnlyCollection<object> args = null, string runId = null)
            => RunAsync("query workflow", c => c.GetWorkflowHandle(workflowId, runId).QueryAsync<TResult>(queryName, args ?? Array.Empty<object>()));

        public Task<OASISResult<TemporalWorkflowInfo>> DescribeWorkflowAsync(string workflowId, string runId = null)
            => RunAsync("describe workflow", async c => ToInfo(await c.GetWorkflowHandle(workflowId, runId).DescribeAsync()));

        /// <summary>Lists executions matching a Temporal visibility query, e.g. "WorkflowType='OasisQuest' AND ExecutionStatus='Running'".</summary>
        public Task<OASISResult<IReadOnlyList<TemporalWorkflowInfo>>> ListWorkflowsAsync(string query, int maxResults = 100)
            => RunAsync<IReadOnlyList<TemporalWorkflowInfo>>("list workflows", async c =>
            {
                var list = new List<TemporalWorkflowInfo>();
                await foreach (var execution in c.ListWorkflowsAsync(query))
                {
                    list.Add(ToInfo(execution));
                    if (list.Count >= maxResults) break;
                }
                return list;
            });

        public Task<OASISResult<TResult>> GetWorkflowResultAsync<TResult>(string workflowId, string runId = null)
            => RunAsync("get workflow result", c => c.GetWorkflowHandle(workflowId, runId).GetResultAsync<TResult>());

        public Task<OASISResult<bool>> CancelWorkflowAsync(string workflowId, string runId = null)
            => RunAsync("cancel workflow", async c => { await c.GetWorkflowHandle(workflowId, runId).CancelAsync(); return true; }, $"Cancellation requested for '{workflowId}'.");

        public Task<OASISResult<bool>> TerminateWorkflowAsync(string workflowId, string reason, string runId = null)
            => RunAsync("terminate workflow", async c => { await c.GetWorkflowHandle(workflowId, runId).TerminateAsync(reason); return true; }, $"Workflow '{workflowId}' terminated.");

        private static TemporalWorkflowInfo ToInfo(WorkflowExecution e) => new()
        {
            WorkflowId = e.Id,
            RunId = e.RunId,
            WorkflowType = e.WorkflowType,
            Status = e.Status.ToString(),
            TaskQueue = e.TaskQueue,
            StartTime = e.StartTime,
            CloseTime = e.CloseTime
        };
    }
}
