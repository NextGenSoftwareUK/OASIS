using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;
using NextGenSoftware.OASIS.API.Core.Helpers;
using Microsoft.AspNetCore.Http;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Holons;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Mfa
{
    /// <summary>
    /// Plugged into AvatarManager.SecondFactorGate. Called after the password (or DID signature) check and BEFORE any token is
    /// issued. Returns null when no second step is needed (always, while MFA is off); otherwise a token-free avatar carrying
    /// metaData.mfaRequired and a single-use metaData.mfaChallengeToken.
    /// </summary>
    public static class MfaGate
    {
        public static Func<IAvatar, string, Task<IAvatar>> Create(MfaService service, IHttpContextAccessor http)
            => async (avatar, ip) =>
            {
                if (service == null || !service.IsEnabled || avatar == null) return null;
                using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(
                    http?.HttpContext?.RequestAborted ?? CancellationToken.None, AvatarLoginReadBudget.CurrentCancellation);
                var appId = http?.HttpContext?.Request.Headers["X-OASIS-App-Id"].ToString();
                if (!await service.NeedsSecondStepAsync(avatar.Id, appId, cancellation.Token).ConfigureAwait(false)) return null;
                var token = await service.CreateChallengeAsync(avatar.Id, ip, cancellation.Token).ConfigureAwait(false);
                return new NextGenSoftware.OASIS.API.Core.Holons.Avatar
                {
                    Id = avatar.Id,
                    Username = avatar.Username,
                    MetaData = new Dictionary<string, object> { [MfaService.RequiredKey] = true, [MfaService.ChallengeKey] = token }
                };
            };

        public static bool IsChallenge(IAvatar avatar)
            => avatar?.MetaData != null && avatar.MetaData.ContainsKey(MfaService.RequiredKey);
    }
}
