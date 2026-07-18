using System;
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
    /// Tests for ONETController singleton caching and endpoint delegation.
    /// Uses reflection to inject a stub ONETManager so OASISBootLoader is never touched.
    /// </summary>
    public class ONETControllerLifecycleTests : IDisposable
    {
        // ── helpers ────────────────────────────────────────────────────────────

        private static void InjectManager(ONETManager manager)
        {
            var field = typeof(ONETController).GetField("_onetManagerTask",
                BindingFlags.Static | BindingFlags.NonPublic)!;
            field.SetValue(null, Task.FromResult(manager));
        }

        private static void ResetStaticManager()
        {
            var field = typeof(ONETController).GetField("_onetManagerTask",
                BindingFlags.Static | BindingFlags.NonPublic)!;
            field.SetValue(null, null);
        }

        private static ONETController BuildController(StubOnetManager manager)
        {
            InjectManager(manager);
            var logger = new Mock<ILogger<ONETController>>();
            var controller = new ONETController(logger.Object);
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            };
            return controller;
        }

        // ── singleton caching ─────────────────────────────────────────────────

        [Fact]
        public void GetOnetManagerStaticAsync_CalledTwice_ReturnsSameTask()
        {
            var stub = new StubOnetManager();
            InjectManager(stub);

            var t1 = ONETController_GetOnetManagerStaticAsync();
            var t2 = ONETController_GetOnetManagerStaticAsync();

            t1.Should().BeSameAs(t2);
        }

        // ── endpoint delegation ───────────────────────────────────────────────

        [Fact]
        public async Task GetNetworkStatus_Returns200()
        {
            var controller = BuildController(new StubOnetManager());

            var result = await controller.GetNetworkStatus();

            result.Should().BeOfType<OkObjectResult>();
        }

        [Fact]
        public async Task StartNetwork_Returns200()
        {
            var controller = BuildController(new StubOnetManager());

            var result = await controller.StartNetwork();

            result.Should().BeOfType<OkObjectResult>();
        }

        [Fact]
        public async Task StopNetwork_Returns200()
        {
            var controller = BuildController(new StubOnetManager());

            var result = await controller.StopNetwork();

            result.Should().BeOfType<OkObjectResult>();
        }

        [Fact]
        public async Task GetNetworkStats_Returns200()
        {
            var controller = BuildController(new StubOnetManager());

            var result = await controller.GetNetworkStats();

            result.Should().BeOfType<OkObjectResult>();
        }

        [Fact]
        public async Task GetNetworkTopology_Returns200()
        {
            var controller = BuildController(new StubOnetManager());

            var result = await controller.GetNetworkTopology();

            result.Should().BeOfType<OkObjectResult>();
        }

        [Fact]
        public async Task ConnectToNode_NullRequest_Returns400()
        {
            var controller = BuildController(new StubOnetManager());

            var result = await controller.ConnectToNode(null!);

            result.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public async Task DisconnectFromNode_NullRequest_Returns400()
        {
            var controller = BuildController(new StubOnetManager());

            var result = await controller.DisconnectFromNode(null!);

            result.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public async Task BroadcastMessage_NullRequest_Returns400()
        {
            var controller = BuildController(new StubOnetManager());

            var result = await controller.BroadcastMessage(null!);

            result.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public async Task GetConnectedNodes_NoSignatureHeaders_Returns200()
        {
            var controller = BuildController(new StubOnetManager());

            var result = await controller.GetConnectedNodes();

            result.Should().BeOfType<OkObjectResult>();
        }

        // ── cleanup ───────────────────────────────────────────────────────────

        public void Dispose() => ResetStaticManager();

        // reflection accessor so we can call the internal static method
        private static Task<ONETManager> ONETController_GetOnetManagerStaticAsync()
        {
            var method = typeof(ONETController).GetMethod("GetOnetManagerStaticAsync",
                BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public)!;
            return (Task<ONETManager>)method.Invoke(null, null)!;
        }

        // ── stub manager ──────────────────────────────────────────────────────

        internal sealed class StubOnetManager : ONETManager
        {
            public StubOnetManager() : base(new OASISDNA()) { }

            public override Task<OASISResult<OASISDNA>> GetOASISDNAAsync()
                => Task.FromResult(new OASISResult<OASISDNA> { Result = new OASISDNA(), IsError = false });

            public override void RegisterNodePublicKey(string nodeId, string publicKey) { }
        }
    }
}
