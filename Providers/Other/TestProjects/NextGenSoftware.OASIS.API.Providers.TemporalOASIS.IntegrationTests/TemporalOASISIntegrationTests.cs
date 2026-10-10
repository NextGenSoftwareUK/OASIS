using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Temporalio.Testing;
using Temporalio.Worker;
using Temporalio.Workflows;

namespace NextGenSoftware.OASIS.API.Providers.TemporalOASIS.IntegrationTests
{
    [Workflow]
    public class QuestWorkflow
    {
        private int _progress;
        private bool _finished;

        [WorkflowRun]
        public async Task<string> RunAsync(string questName)
        {
            await Workflow.WaitConditionAsync(() => _finished);
            return $"{questName} completed at {_progress}%";
        }

        [WorkflowSignal]
        public Task ProgressAsync(int percent)
        {
            _progress = percent;
            _finished = percent >= 100;
            return Task.CompletedTask;
        }

        [WorkflowQuery]
        public int Progress() => _progress;
    }

    /// <summary>Runs against Temporal's dev server, which the SDK downloads and starts on first use.</summary>
    [TestClass]
    public class TemporalOASISIntegrationTests
    {
        private const string TaskQueue = "oasis-tests";
        private static WorkflowEnvironment _env;

        [ClassInitialize]
        public static async Task Start(TestContext _) => _env = await WorkflowEnvironment.StartLocalAsync();

        [ClassCleanup]
        public static async Task Stop() { if (_env != null) await _env.ShutdownAsync(); }

        [TestMethod]
        public async Task Workflow_lifecycle_runs_through_the_provider()
        {
            using var worker = new TemporalWorker(_env.Client, new TemporalWorkerOptions(TaskQueue).AddWorkflow<QuestWorkflow>());
            await worker.ExecuteAsync(async () =>
            {
                var provider = new TemporalOASIS(_env.Client, TaskQueue);
                Assert.IsFalse((await provider.ActivateProviderAsync()).IsError);

                var id = $"quest-{Guid.NewGuid():N}";
                var started = await provider.StartWorkflowAsync("QuestWorkflow", id, new object[] { "Find the Oracle" });
                Assert.IsFalse(started.IsError, started.Message);

                Assert.IsFalse((await provider.SignalWorkflowAsync(id, "Progress", new object[] { 40 })).IsError);
                var progress = await provider.QueryWorkflowAsync<int>(id, "Progress");
                Assert.AreEqual(40, progress.Result);

                var described = await provider.DescribeWorkflowAsync(id);
                Assert.AreEqual("Running", described.Result.Status);
                Assert.AreEqual("QuestWorkflow", described.Result.WorkflowType);

                await provider.SignalWorkflowAsync(id, "Progress", new object[] { 100 });
                var result = await provider.GetWorkflowResultAsync<string>(id);
                Assert.AreEqual("Find the Oracle completed at 100%", result.Result);

                var listed = await provider.ListWorkflowsAsync($"WorkflowId='{id}'");
                Assert.IsFalse(listed.IsError, listed.Message);
            });
        }

        [TestMethod]
        public async Task Operations_on_unknown_workflows_are_errors()
        {
            var provider = new TemporalOASIS(_env.Client, TaskQueue);
            var described = await provider.DescribeWorkflowAsync("does-not-exist");
            Assert.IsTrue(described.IsError);
        }
    }
}
