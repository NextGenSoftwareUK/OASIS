using System.Net;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NextGenSoftware.OASIS.API.Core.Configuration;
using NextGenSoftware.OASIS.API.Core.Managers.OASISHyperDrive.Synchronization;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Controllers;
using NextGenSoftware.OASIS.Common;
using Xunit;
using CoreAvatar = NextGenSoftware.OASIS.API.Core.Holons.Avatar;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.UnitTests;

public sealed class HyperDrivePeerBindingControllerTests : IDisposable
{
    private readonly OASISDNA? _priorDna = OASISBootLoader.OASISBootLoader.OASISDNA;

    [Fact]
    public async Task BindingFailsClosedWhenHostedSyncIsDisabled()
    {
        OASISBootLoader.OASISBootLoader.OASISDNA = Dna(enabled: false);
        var response = await Controller(Guid.NewGuid()).BindOnetPeer(new BindHyperDrivePeerRequest(), default);

        AssertError(response, HttpStatusCode.ServiceUnavailable, "HOSTED_SYNC_DISABLED");
    }

    [Fact]
    public async Task BindingRejectsIncompleteRequestBeforeProviderAccess()
    {
        EnableHostedSync();
        var response = await Controller(Guid.NewGuid()).BindOnetPeer(new BindHyperDrivePeerRequest
        {
            DeviceId = Guid.NewGuid()
        }, default);

        AssertError(response, HttpStatusCode.BadRequest, "HOSTED_SYNC_PEER_BINDING_INVALID");
    }

    [Fact]
    public async Task BindingRejectsNonBase64IdentityMaterialBeforeProviderAccess()
    {
        EnableHostedSync();
        var response = await Controller(Guid.NewGuid()).BindOnetPeer(new BindHyperDrivePeerRequest
        {
            DeviceId = Guid.NewGuid(), NodeId = "node", PublicKey = "not-base64", Signature = "not-base64"
        }, default);

        AssertError(response, HttpStatusCode.BadRequest, "HOSTED_SYNC_PEER_BINDING_ENCODING_INVALID");
    }

    [Fact]
    public async Task BindingRejectsNodeIdNotDerivedFromSubmittedPublicKey()
    {
        EnableHostedSync();
        using ECDsa key = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        var response = await Controller(Guid.NewGuid()).BindOnetPeer(new BindHyperDrivePeerRequest
        {
            DeviceId = Guid.NewGuid(), NodeId = new string('0', 64),
            PublicKey = Convert.ToBase64String(key.ExportSubjectPublicKeyInfo()),
            Signature = Convert.ToBase64String(new byte[64])
        }, default);

        AssertError(response, HttpStatusCode.BadRequest, "HOSTED_SYNC_PEER_NODE_ID_INVALID");
    }

    [Fact]
    public async Task BindingRejectsProofNotSignedForAuthenticatedAvatarAndDevice()
    {
        EnableHostedSync();
        Guid authenticatedAvatarId = Guid.NewGuid();
        Guid deviceId = Guid.NewGuid();
        using ECDsa key = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        byte[] publicKey = key.ExportSubjectPublicKeyInfo();
        string nodeId = HyperDrivePeerBindingProof.DeriveNodeId(publicKey);
        byte[] wrongProof = key.SignData(System.Text.Encoding.UTF8.GetBytes(
            HyperDrivePeerBindingProof.BuildMessage(Guid.NewGuid(), deviceId, nodeId)),
            HashAlgorithmName.SHA256);

        var response = await Controller(authenticatedAvatarId).BindOnetPeer(new BindHyperDrivePeerRequest
        {
            DeviceId = deviceId, NodeId = nodeId, PublicKey = Convert.ToBase64String(publicKey),
            Signature = Convert.ToBase64String(wrongProof)
        }, default);

        AssertError(response, HttpStatusCode.Forbidden, "HOSTED_SYNC_PEER_PROOF_INVALID");
    }

    public void Dispose() => OASISBootLoader.OASISBootLoader.OASISDNA = _priorDna;

    private static void EnableHostedSync() => OASISBootLoader.OASISBootLoader.OASISDNA = Dna(enabled: true);

    private static OASISDNA Dna(bool enabled) => new()
    {
        OASIS = new NextGenSoftware.OASIS.API.DNA.OASIS
        {
            OASISHyperDriveConfig = new OASISHyperDriveConfig { EnableHostedSync = enabled }
        }
    };

    private static HyperDriveSyncController Controller(Guid avatarId)
    {
        var controller = new HyperDriveSyncController
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };
        controller.HttpContext.Items["Avatar"] = new CoreAvatar { AvatarId = avatarId };
        return controller;
    }

    private static void AssertError(IActionResult action, HttpStatusCode status, string errorCode)
    {
        var result = Assert.IsType<ObjectResult>(action);
        Assert.Equal((int)status, result.StatusCode);
        var payload = Assert.IsType<OASISResult<bool>>(result.Value);
        Assert.True(payload.IsError);
        Assert.Equal(errorCode, payload.ErrorCode);
    }
}
