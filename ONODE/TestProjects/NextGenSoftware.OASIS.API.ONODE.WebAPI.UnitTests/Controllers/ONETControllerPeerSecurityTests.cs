using System;
using System.Reflection;
using System.Security.Cryptography;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.API.ONODE.Core.Managers;
using NextGenSoftware.OASIS.API.ONODE.Core.Network;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Controllers;
using NextGenSoftware.OASIS.Common;
using Xunit;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.UnitTests.Controllers
{
    [Collection(ONETControllerSingletonCollection.Name)]
    public class ONETControllerPeerSecurityTests : IDisposable
    {
        private static readonly FieldInfo ManagerTaskField =
            typeof(ONETController).GetField("_onetManagerTask", BindingFlags.Static | BindingFlags.NonPublic)!;

        private static (string NodeId, string PublicKey) NewIdentity()
        {
            using var ecdsa = ECDsa.Create(ECCurve.NamedCurves.nistP256);
            var publicKey = Convert.ToBase64String(ecdsa.ExportSubjectPublicKeyInfo());
            return (ONETSecurity.DeriveNodeId(publicKey)!, publicKey);
        }

        private static ONETController BuildController(bool signatureValid, Action<HttpRequest>? configure = null)
        {
            var dna = new OASISDNA();
            dna.OASIS.ONET = new ONETConfig { ONETApiKey = "" };
            ManagerTaskField.SetValue(null, Task.FromResult<ONETManager>(new FakeOnetManager(dna, signatureValid)));

            var httpContext = new DefaultHttpContext();
            configure?.Invoke(httpContext.Request);
            return new ONETController(new Mock<ILogger<ONETController>>().Object)
            {
                ControllerContext = new ControllerContext { HttpContext = httpContext }
            };
        }

        [Fact]
        public async Task GetPeers_WithoutSignatureHeaders_Returns401()
        {
            var controller = BuildController(signatureValid: true);

            (await controller.GetPeers()).Should().BeOfType<UnauthorizedObjectResult>();
        }

        [Fact]
        public async Task GetPeers_InvalidOrReplayedSignature_Returns401()
        {
            var controller = BuildController(signatureValid: false, r =>
            {
                r.Headers["X-ONET-NodeId"] = "abc";
                r.Headers["X-ONET-Timestamp"] = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
                r.Headers["X-ONET-Signature"] = "sig";
            });

            (await controller.GetPeers()).Should().BeOfType<UnauthorizedObjectResult>();
        }

        [Fact]
        public async Task RegisterNode_NodeIdNotDerivedFromKey_Returns400()
        {
            var controller = BuildController(signatureValid: true);
            var (_, publicKey) = NewIdentity();

            var result = await controller.RegisterNode(new RegisterNodeRequest { NodeId = "victim", PublicKey = publicKey });

            result.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public async Task RegisterNode_AddressWithoutProofOfKey_Returns401()
        {
            var controller = BuildController(signatureValid: false);
            var (nodeId, publicKey) = NewIdentity();

            var result = await controller.RegisterNode(new RegisterNodeRequest
            {
                NodeId = nodeId,
                PublicKey = publicKey,
                NodeAddress = "203.0.113.5:38470"
            });

            result.Should().BeOfType<UnauthorizedObjectResult>();
        }

        [Fact]
        public async Task RegisterNode_KeyOnly_Returns200()
        {
            var controller = BuildController(signatureValid: false);
            var (nodeId, publicKey) = NewIdentity();

            (await controller.RegisterNode(new RegisterNodeRequest { NodeId = nodeId, PublicKey = publicKey }))
                .Should().BeOfType<OkObjectResult>();
        }

        public void Dispose() => ManagerTaskField.SetValue(null, null);

        private sealed class FakeOnetManager : ONETManager
        {
            private readonly OASISDNA _dna;
            private readonly bool _signatureValid;

            public FakeOnetManager(OASISDNA dna, bool signatureValid) : base(dna)
            {
                _dna = dna;
                _signatureValid = signatureValid;
            }

            public override Task<OASISResult<OASISDNA>> GetOASISDNAAsync()
                => Task.FromResult(new OASISResult<OASISDNA> { Result = _dna });

            public override bool RegisterNodePublicKey(string nodeId, string publicKey)
                => ONETSecurity.DeriveNodeId(publicKey) == nodeId;

            public override Task<bool> VerifyFreshRequestSignatureAsync(string nodeId, string purpose, long unixSeconds, string base64Signature)
                => Task.FromResult(_signatureValid);
        }
    }

    [CollectionDefinition(Name)]
    public class ONETControllerSingletonCollection
    {
        public const string Name = "ONETController static manager singleton";
    }
}
