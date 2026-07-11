using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NextGenSoftware.OASIS.Web6.Core.Managers;

namespace NextGenSoftware.OASIS.Web6.WebAPI.Controllers
{
    /// <summary>
    /// Decentralised Identity (DID) and Verifiable Credential (VC) endpoints.
    /// Priority 20 — DID/VCs.
    /// </summary>
    [ApiController]
    [Route("v1/auth")]
    public class DidController : Web6ControllerBase
    {
        /// <summary>
        /// Creates or retrieves the DID document for the authenticated avatar.
        /// POST /v1/auth/did
        /// </summary>
        [HttpPost("did")]
        [ProducesResponseType(typeof(DidDocument), StatusCodes.Status200OK)]
        public IActionResult CreateDid([FromBody] DidRequest? request = null)
        {
            Guid avatarId = request?.AvatarId != null && request.AvatarId != Guid.Empty ? request.AvatarId : AvatarId;
            if (avatarId == Guid.Empty)
                return BadRequest(new { error = "AvatarId is required (set via JWT or request body)" });

            var manager = new DidManager(avatarId, OASISDNA);
            var result = manager.CreateDid(avatarId);
            return result.IsError ? BadRequest(result) : Ok(result.Result);
        }

        /// <summary>
        /// Resolves a DID document from the Universal Resolver.
        /// GET /v1/auth/did/{did}
        /// </summary>
        [HttpGet("did/{*did}")]
        [ProducesResponseType(typeof(DidDocument), StatusCodes.Status200OK)]
        public async Task<IActionResult> ResolveDid(string did)
        {
            var manager = new DidManager(AvatarId, OASISDNA);
            var result = await manager.ResolveDid(did);
            return result.IsError ? BadRequest(result) : Ok(result.Result);
        }

        /// <summary>
        /// Issues a Verifiable Credential for the authenticated avatar.
        /// POST /v1/auth/vc
        /// </summary>
        [HttpPost("vc")]
        [ProducesResponseType(typeof(VerifiableCredential), StatusCodes.Status200OK)]
        public IActionResult IssueVc([FromBody] VcIssueRequest request)
        {
            if (string.IsNullOrWhiteSpace(request?.SubjectDid))
                return BadRequest(new { error = "subjectDid is required" });

            Guid avatarId = AvatarId;
            if (avatarId == Guid.Empty)
                return Unauthorized(new { error = "JWT required to issue credentials" });

            var manager = new DidManager(avatarId, OASISDNA);
            var result = manager.IssueCredential(avatarId, request.SubjectDid, request.Claims ?? new Dictionary<string, object>());
            return result.IsError ? BadRequest(result) : Ok(result.Result);
        }

        /// <summary>
        /// Verifies a Verifiable Credential issued by this system.
        /// POST /v1/auth/vc/verify
        /// </summary>
        [HttpPost("vc/verify")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        public IActionResult VerifyVc([FromBody] VcVerifyRequest request)
        {
            if (request?.Credential == null)
                return BadRequest(new { error = "credential is required" });
            if (request.IssuerAvatarId == Guid.Empty)
                return BadRequest(new { error = "issuerAvatarId is required" });

            var manager = new DidManager(AvatarId, OASISDNA);
            var result = manager.VerifyCredential(request.Credential, request.IssuerAvatarId);
            return result.IsError
                ? BadRequest(result)
                : Ok(new { valid = result.Result });
        }
    }

    public class DidRequest
    {
        public Guid AvatarId { get; set; }
    }

    public class VcIssueRequest
    {
        public string SubjectDid { get; set; }
        public Dictionary<string, object> Claims { get; set; }
    }

    public class VcVerifyRequest
    {
        public VerifiableCredential Credential { get; set; }
        public Guid IssuerAvatarId { get; set; }
    }
}
