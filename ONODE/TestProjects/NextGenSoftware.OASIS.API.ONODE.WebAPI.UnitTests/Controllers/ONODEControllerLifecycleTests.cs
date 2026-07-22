using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.API.ONODE.Core.Managers;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Controllers;
using NextGenSoftware.OASIS.Common;
using Xunit;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.UnitTests.Controllers
{
    /// <summary>
    /// Tests for ONODEController singleton caching and endpoint delegation.
    /// Injects a stub ONODEManager via reflection to avoid touching OASISBootLoader.
    /// </summary>
    public class ONODEControllerLifecycleTests : IDisposable
    {
        // ── helpers ────────────────────────────────────────────────────────────

        private static void InjectManager(ONODEManager manager)
        {
            var field = typeof(ONODEController).GetField("_onodeManagerTask",
                BindingFlags.Static | BindingFlags.NonPublic)!;
            field.SetValue(null, Task.FromResult(manager));
        }

        private static void ResetStaticManager()
        {
            var field = typeof(ONODEController).GetField("_onodeManagerTask",
                BindingFlags.Static | BindingFlags.NonPublic)!;
            field.SetValue(null, null);
        }

        private static ONODEController BuildController()
        {
            var manager = new StubOnodeManager();
            InjectManager(manager);
            var logger = new Mock<ILogger<ONODEController>>();
            var controller = new ONODEController(logger.Object);
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            };
            return controller;
        }

        // ── singleton caching ─────────────────────────────────────────────────

        [Fact]
        public void GetOnodeManagerAsync_CalledTwice_ReturnsSameTask()
        {
            var stub = new StubOnodeManager();
            InjectManager(stub);

            var t1 = ONODEController_GetOnodeManagerAsync();
            var t2 = ONODEController_GetOnodeManagerAsync();

            t1.Should().BeSameAs(t2);
        }

        // ── lifecycle endpoints ───────────────────────────────────────────────

        [Fact]
        public async Task GetNodeStatus_Returns200()
        {
            var result = await BuildController().GetNodeStatus();
            result.Should().BeOfType<OkObjectResult>();
        }

        [Fact]
        public async Task GetNodeInfo_Returns200()
        {
            var result = await BuildController().GetNodeInfo();
            result.Should().BeOfType<OkObjectResult>();
        }

        [Fact]
        public async Task StartNode_Returns200()
        {
            var result = await BuildController().StartNode();
            result.Should().BeOfType<OkObjectResult>();
        }

        [Fact]
        public async Task StopNode_WhenNotRunning_Returns400()
        {
            // Node is not started — StopNodeAsync returns IsError, controller maps to 400.
            var result = await BuildController().StopNode();
            result.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public async Task RestartNode_Returns200()
        {
            var result = await BuildController().RestartNode();
            result.Should().BeOfType<OkObjectResult>();
        }

        [Fact]
        public async Task GetNodeMetrics_Returns200()
        {
            var result = await BuildController().GetNodeMetrics();
            result.Should().BeOfType<OkObjectResult>();
        }

        [Fact]
        public async Task GetNodeLogs_Returns200()
        {
            var result = await BuildController().GetNodeLogs(50);
            result.Should().BeOfType<OkObjectResult>();
        }

        [Fact]
        public async Task GetConnectedPeers_Returns200()
        {
            var result = await BuildController().GetConnectedPeers();
            result.Should().BeOfType<OkObjectResult>();
        }

        [Fact]
        public async Task GetNodeStats_Returns200()
        {
            var result = await BuildController().GetNodeStats();
            result.Should().BeOfType<OkObjectResult>();
        }

        [Fact]
        public async Task GetNodeConfig_Returns200()
        {
            var result = await BuildController().GetNodeConfig();
            result.Should().BeOfType<OkObjectResult>();
        }

        [Fact]
        public async Task UpdateNodeConfig_NullRequest_Returns400()
        {
            var result = await BuildController().UpdateNodeConfig(null!);
            result.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public async Task UpdateOASISDNA_NullRequest_Returns400()
        {
            var result = await BuildController().UpdateOASISDNA(null!);
            result.Should().BeOfType<BadRequestObjectResult>();
        }

        // ── cleanup ───────────────────────────────────────────────────────────

        public void Dispose() => ResetStaticManager();

        private static Task<ONODEManager> ONODEController_GetOnodeManagerAsync()
        {
            var method = typeof(ONODEController).GetMethod("GetOnodeManagerAsync",
                BindingFlags.Static | BindingFlags.NonPublic)!;
            return (Task<ONODEManager>)method.Invoke(null, null)!;
        }

        // ── stub manager ──────────────────────────────────────────────────────

        internal sealed class StubOnodeManager : ONODEManager
        {
            public StubOnodeManager() : base(null, new OASISDNA()) { }
        }
    }
}
