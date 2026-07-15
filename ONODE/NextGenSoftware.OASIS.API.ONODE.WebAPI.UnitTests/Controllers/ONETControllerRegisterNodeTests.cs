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
using NextGenSoftware.OASIS.API.ONODE.Core.Network;
using NextGenSoftware.OASIS.API.ONODE.Core.Managers;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Controllers;
using NextGenSoftware.OASIS.Common;
using Xunit;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.UnitTests.Controllers
{
    /// <summary>
    /// Tests for POST /onet/nodes/register API key authentication logic.
    /// Uses reflection to inject a mock ONETManager singleton so OASISBootLoader is never touched.
    /// </summary>
    public class ONETControllerRegisterNodeTests : IDisposable
    {
        public ONETControllerRegisterNodeTests() { }

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

        private static ONETController BuildController(string? apiKey, string? suppliedHeader = null)
        {
            var logger = new Mock<ILogger<ONETController>>();
            var controller = new ONETController(logger.Object);

            var dna = new OASISDNA();
            dna.OASIS.ONET = new ONETConfig { ONETApiKey = apiKey ?? "" };

            var fake = new FakeOnetManager(dna);
            InjectManager(fake);

            var httpCtx = new DefaultHttpContext();
            if (suppliedHeader != null)
                httpCtx.Request.Headers["X-ONET-API-Key"] = suppliedHeader;
            controller.ControllerContext = new ControllerContext { HttpContext = httpCtx };

            return controller;
        }

        // ── tests ──────────────────────────────────────────────────────────────

        [Fact]
        public async Task RegisterNode_NoApiKeyConfigured_OpenRegistration_Returns200()
        {
            var controller = BuildController(apiKey: "", suppliedHeader: null);

            var result = await controller.RegisterNode(new RegisterNodeRequest
            {
                NodeId = "node-001",
                PublicKey = Convert.ToBase64String(new byte[32])
            });

            result.Should().BeOfType<OkObjectResult>();
        }

        [Fact]
        public async Task RegisterNode_CorrectApiKey_Returns200()
        {
            const string key = "super-secret-key";
            var controller = BuildController(apiKey: key, suppliedHeader: key);

            var result = await controller.RegisterNode(new RegisterNodeRequest
            {
                NodeId = "node-002",
                PublicKey = Convert.ToBase64String(new byte[32])
            });

            result.Should().BeOfType<OkObjectResult>();
        }

        [Fact]
        public async Task RegisterNode_WrongApiKey_Returns401()
        {
            var controller = BuildController(apiKey: "correct-key", suppliedHeader: "wrong-key");

            var result = await controller.RegisterNode(new RegisterNodeRequest
            {
                NodeId = "node-003",
                PublicKey = Convert.ToBase64String(new byte[32])
            });

            result.Should().BeOfType<UnauthorizedObjectResult>();
        }

        [Fact]
        public async Task RegisterNode_MissingApiKeyHeader_Returns401()
        {
            var controller = BuildController(apiKey: "required-key", suppliedHeader: null);

            var result = await controller.RegisterNode(new RegisterNodeRequest
            {
                NodeId = "node-004",
                PublicKey = Convert.ToBase64String(new byte[32])
            });

            result.Should().BeOfType<UnauthorizedObjectResult>();
        }

        [Fact]
        public async Task RegisterNode_NullRequest_Returns400()
        {
            var controller = BuildController(apiKey: "", suppliedHeader: null);

            var result = await controller.RegisterNode(null!);

            result.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public async Task RegisterNode_EmptyNodeId_Returns400()
        {
            var controller = BuildController(apiKey: "", suppliedHeader: null);

            var result = await controller.RegisterNode(new RegisterNodeRequest
            {
                NodeId = "",
                PublicKey = Convert.ToBase64String(new byte[32])
            });

            result.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public async Task RegisterNode_EmptyPublicKey_Returns400()
        {
            var controller = BuildController(apiKey: "", suppliedHeader: null);

            var result = await controller.RegisterNode(new RegisterNodeRequest
            {
                NodeId = "node-005",
                PublicKey = ""
            });

            result.Should().BeOfType<BadRequestObjectResult>();
        }

        public void Dispose() => ResetStaticManager();

        // ── fake manager ──────────────────────────────────────────────────────

        private sealed class FakeOnetManager : ONETManager
        {
            private readonly OASISDNA _dna;

            public FakeOnetManager(OASISDNA dna)
                : base(dna)   // protected test constructor — no network init
            {
                _dna = dna;
            }

            public override Task<OASISResult<OASISDNA>> GetOASISDNAAsync()
                => Task.FromResult(new OASISResult<OASISDNA> { Result = _dna, IsError = false });

            public override void RegisterNodePublicKey(string nodeId, string publicKey) { }
        }
    }
}
