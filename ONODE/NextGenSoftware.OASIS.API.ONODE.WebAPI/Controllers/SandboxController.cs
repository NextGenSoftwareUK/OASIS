using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Controllers
{
    /// <summary>
    /// WEB4 sandbox controls. Exists only on the sandbox deployment (OASIS_SANDBOX=true); answers 404 everywhere else.
    /// </summary>
    [Route("api/sandbox")]
    [ApiController]
    public class SandboxController : OASISControllerBase
    {
        /// <summary>How long a provider stays offline before it restores itself, so a demo can never leave it down.</summary>
        public static readonly TimeSpan MaxOfflineDuration = TimeSpan.FromMinutes(2);

        /// <summary>
        /// Creates a fresh, verified demo avatar for this visitor and signs it in. Sandbox avatars live only in the
        /// sandbox's own databases, so visitors can try everything without an OASIS account.
        /// </summary>
        [HttpPost("session")]
        public async Task<ActionResult<OASISResult<IAvatar>>> StartSession()
        {
            if (!ProviderFaultGate.IsEnabled)
                return NotFound();

            string suffix = Convert.ToHexString(RandomNumberGenerator.GetBytes(4)).ToLowerInvariant();
            string username = $"sandbox-{suffix}";
            string password = Convert.ToBase64String(RandomNumberGenerator.GetBytes(24));

            OASISResult<IAvatar> registerResult = await Program.AvatarManager.RegisterAsync("Explorer", "Sandbox", suffix,
                $"{username}@sandbox.oasisomniverse.one", password, username, AvatarType.User, OASISType.OASISAPIREST, suppressVerificationEmail: true);

            if (registerResult.IsError || registerResult.Result == null)
                return BadRequest(new OASISResult<IAvatar> { IsError = true, Message = $"Could not create a sandbox avatar: {registerResult.Message}" });

            IAvatar avatar = registerResult.Result;
            avatar.Verified = DateTime.UtcNow;
            avatar.VerificationToken = null;
            avatar.IsActive = true;
            OASISResult<IAvatar> saveResult = await Program.AvatarManager.SaveAvatarAsync(avatar);

            if (saveResult.IsError)
                return BadRequest(new OASISResult<IAvatar> { IsError = true, Message = $"Could not verify the sandbox avatar: {saveResult.Message}" });

            OASISResult<IAvatar> authResult = await Program.AvatarManager.AuthenticateAsync(username, password, HttpContext.Connection.RemoteIpAddress?.ToString());

            if (authResult.IsError || authResult.Result == null)
                return BadRequest(new OASISResult<IAvatar> { IsError = true, Message = $"Could not sign in the sandbox avatar: {authResult.Message}" });

            return Ok(authResult);
        }

        /// <summary>Lists providers currently taken offline and when each comes back.</summary>
        [HttpGet("faults")]
        public ActionResult<OASISResult<Dictionary<string, DateTime>>> GetFaults()
        {
            if (!ProviderFaultGate.IsEnabled)
                return NotFound();

            return Ok(new OASISResult<Dictionary<string, DateTime>>
            {
                Result = ProviderFaultGate.GetFaulted().ToDictionary(x => x.Key.ToString(), x => x.Value),
                Message = "Sandbox provider faults retrieved."
            });
        }

        /// <summary>Takes a provider offline for up to two minutes so HyperDrive must fail over.</summary>
        [Authorize]
        [HttpPost("providers/{providerType}/offline")]
        public ActionResult<OASISResult<DateTime>> TakeOffline(ProviderType providerType, [FromQuery] int seconds = 60)
        {
            if (!ProviderFaultGate.IsEnabled)
                return NotFound();

            if (!ProviderManager.Instance.GetAllRegisteredProviders().Any(x => x.ProviderType.Value == providerType))
                return BadRequest(new OASISResult<DateTime> { IsError = true, Message = $"{providerType} is not registered on the sandbox." });

            TimeSpan duration = TimeSpan.FromSeconds(Math.Clamp(seconds, 5, (int)MaxOfflineDuration.TotalSeconds));
            ProviderFaultGate.Fault(providerType, duration);

            return Ok(new OASISResult<DateTime>
            {
                Result = DateTime.UtcNow.Add(duration),
                Message = $"{providerType} is offline for {duration.TotalSeconds:0} seconds."
            });
        }

        /// <summary>Brings a provider back online immediately.</summary>
        [Authorize]
        [HttpPost("providers/{providerType}/online")]
        public ActionResult<OASISResult<bool>> BringOnline(ProviderType providerType)
        {
            if (!ProviderFaultGate.IsEnabled)
                return NotFound();

            ProviderFaultGate.Restore(providerType);
            return Ok(new OASISResult<bool> { Result = true, Message = $"{providerType} is back online." });
        }
    }
}
