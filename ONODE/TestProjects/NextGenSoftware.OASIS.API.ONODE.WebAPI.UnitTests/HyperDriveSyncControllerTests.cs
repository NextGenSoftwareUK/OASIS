using System.Net;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NextGenSoftware.OASIS.API.Core.Configuration;
using NextGenSoftware.OASIS.API.Core.Managers.OASISHyperDrive.Synchronization;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Controllers;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Middleware;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Services;
using NextGenSoftware.OASIS.Common;
using Xunit;
using CoreAvatar = NextGenSoftware.OASIS.API.Core.Holons.Avatar;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.UnitTests;

public sealed class HyperDriveSyncControllerTests : IDisposable
{
    private readonly OASISDNA? _priorDna = OASISBootLoader.OASISBootLoader.OASISDNA;

    [Fact]
    public async Task ExchangeFailsClosedWhenHostedSyncIsDisabled()
    {
        SetHostedSync(false);
        IActionResult response = await Controller(Guid.NewGuid()).Exchange(new SyncExchangeRequest(), default);

        AssertError(response, HttpStatusCode.ServiceUnavailable, "HOSTED_SYNC_DISABLED");
    }

    [Fact]
    public async Task InvalidBearerCannotDowngradeToOfflineGrantAuthentication()
    {
        SetHostedSync(true);
        var issuer = new StubIssuer(new OASISResult<Guid>(Guid.NewGuid()));
        var controller = Controller(Guid.Empty, issuer);
        controller.HttpContext.Items[JwtMiddleware.AuthenticationErrorItemKey] = "invalid bearer";

        IActionResult response = await controller.Exchange(new SyncExchangeRequest
        {
            DeviceId = Guid.NewGuid(), OfflineSessionGrant = new HyperDriveOfflineSessionGrant()
        }, default);

        AssertError(response, HttpStatusCode.Unauthorized, "HOSTED_SYNC_BEARER_INVALID");
        Assert.Equal(0, issuer.ValidationCount);
    }

    [Fact]
    public async Task AnonymousExchangeRequiresOfflineGrantIssuer()
    {
        SetHostedSync(true);
        IActionResult response = await Controller(Guid.Empty).Exchange(new SyncExchangeRequest(), default);

        AssertError(response, HttpStatusCode.Unauthorized, "HOSTED_SYNC_AUTH_REQUIRED");
    }

    [Fact]
    public async Task OfflineGrantValidationErrorIsPreservedAtApiBoundary()
    {
        SetHostedSync(true);
        var issuer = new StubIssuer(new OASISResult<Guid>
        {
            IsError = true, ErrorCount = 1, ErrorCode = "OFFLINE_GRANT_EXPIRED", Message = "expired"
        });
        Guid deviceId = Guid.NewGuid();

        IActionResult response = await Controller(Guid.Empty, issuer).Exchange(new SyncExchangeRequest
        {
            DeviceId = deviceId,
            OfflineSessionGrant = new HyperDriveOfflineSessionGrant { DeviceId = deviceId }
        }, default);

        AssertError(response, HttpStatusCode.Unauthorized, "OFFLINE_GRANT_EXPIRED");
        Assert.Equal(1, issuer.ValidationCount);
        Assert.Equal("hyperdrive.sync", issuer.RequiredScope);
        Assert.Equal(deviceId, issuer.DeviceId);
    }

    [Fact]
    public async Task BearerIdentityCannotSubmitAnotherAvatarsOfflineGrant()
    {
        SetHostedSync(true);
        Guid authenticatedAvatar = Guid.NewGuid();
        IActionResult response = await Controller(authenticatedAvatar).Exchange(new SyncExchangeRequest
        {
            DeviceId = Guid.NewGuid(),
            OfflineSessionGrant = new HyperDriveOfflineSessionGrant { AvatarId = Guid.NewGuid() }
        }, default);

        AssertError(response, HttpStatusCode.Forbidden, "HOSTED_SYNC_OFFLINE_GRANT_AVATAR_MISMATCH");
    }

    public void Dispose() => OASISBootLoader.OASISBootLoader.OASISDNA = _priorDna;

    private static void SetHostedSync(bool enabled) => OASISBootLoader.OASISBootLoader.OASISDNA = new OASISDNA
    {
        OASIS = new NextGenSoftware.OASIS.API.DNA.OASIS
        {
            OASISHyperDriveConfig = new OASISHyperDriveConfig { EnableHostedSync = enabled }
        }
    };

    private static HyperDriveSyncController Controller(Guid avatarId,
        IHyperDriveOfflineSessionGrantIssuer? issuer = null)
    {
        var controller = new HyperDriveSyncController(issuer)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };
        if (avatarId != Guid.Empty)
            controller.HttpContext.Items["Avatar"] = new CoreAvatar { AvatarId = avatarId };
        return controller;
    }

    private static void AssertError(IActionResult action, HttpStatusCode status, string errorCode)
    {
        var result = Assert.IsType<ObjectResult>(action);
        Assert.Equal((int)status, result.StatusCode);
        var payload = Assert.IsType<OASISResult<SyncExchangeResponse>>(result.Value);
        Assert.True(payload.IsError);
        Assert.Equal(errorCode, payload.ErrorCode);
    }

    private sealed class StubIssuer : IHyperDriveOfflineSessionGrantIssuer
    {
        private readonly OASISResult<Guid> _validation;

        public StubIssuer(OASISResult<Guid> validation) => _validation = validation;

        public int ValidationCount { get; private set; }
        public string? RequiredScope { get; private set; }
        public Guid DeviceId { get; private set; }

        public Task<OASISResult<HyperDriveOfflineSessionGrant>> IssueAsync(Guid authenticatedAvatarId,
            IssueHyperDriveOfflineSessionGrantRequest request, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<OASISResult<Guid>> ValidateAsync(HyperDriveOfflineSessionGrant grant, string requiredScope,
            Guid requestDeviceId, CancellationToken cancellationToken)
        {
            ValidationCount++;
            RequiredScope = requiredScope;
            DeviceId = requestDeviceId;
            return Task.FromResult(_validation);
        }
    }
}
