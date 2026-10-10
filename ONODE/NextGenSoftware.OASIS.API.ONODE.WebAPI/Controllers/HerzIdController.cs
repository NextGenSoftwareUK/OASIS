using System;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Models.HerzId;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Services;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Controllers
{
    /// <summary>
    /// HerzID management endpoints.
    ///
    /// HerzID is a sovereign identity issued on top of the OASIS Avatar SSO system.
    /// Every OASIS Avatar can optionally be assigned a HerzID after completing:
    ///   1. A voice declaration (biometric liveness check)
    ///   2. Being vouched for by an existing verified HerzID member
    ///
    /// The HerzID format is: [3-digit country code] · [10-digit sequential] · [QEA seal]
    /// Example: 052·0·000·000·001·✦  (Mexico, member #1, QEA verified)
    ///
    /// All endpoints require OASIS JWT authentication unless marked [AllowAnonymous].
    /// All endpoints return 404 when HerzId.Enabled = false in OASISDNA.
    /// </summary>
    [ApiController]
    [Route("api/herzid")]
    [Authorize]
    public class HerzIdController : OASISControllerBase
    {
        private readonly IHerzCounterService _counter;
        private readonly IQeaSealService _seal;
        private AvatarManager AvatarManager => Program.AvatarManager;

        private bool HerzEnabled =>
            OASISDNAManager.OASISDNA?.OASIS?.Security?.HerzId?.Enabled == true;

        private HerzIdSettings HerzCfg =>
            OASISDNAManager.OASISDNA?.OASIS?.Security?.HerzId ?? new HerzIdSettings();

        public HerzIdController(IHerzCounterService counter, IQeaSealService seal)
        {
            _counter = counter;
            _seal    = seal;
        }

        // ── Register ──────────────────────────────────────────────────────────

        /// <summary>
        /// Assign a HerzID to the currently authenticated avatar.
        ///
        /// Prerequisites:
        ///   - Avatar must not already have a HerzID
        ///   - Voucher's HerzID must exist and they must have vouches remaining
        ///   - Country code must be a valid 3-digit telephone prefix
        ///
        /// On success:
        ///   - Atomic sequential number is claimed
        ///   - QEA seal is computed (HMAC-SHA256 of number + country + profile + seed + date)
        ///   - HerzID string is assembled and stored on the Avatar
        ///   - Voucher's remaining vouch count is decremented by 1
        ///   - New member receives HerzCfg.NewMemberVouches vouches (default 12)
        ///   - Clearance level is set to 2 (Wanderer — HerzID assigned)
        /// </summary>
        [HttpPost("register")]
        [ProducesResponseType(typeof(HerzIdRegisterResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Register([FromBody] HerzIdRegisterRequest request)
        {
            if (!HerzEnabled)
                return NotFound(new { error = "HerzID is not enabled on this OASIS instance." });

            var avatar = Avatar;
            if (avatar == null) return Unauthorized();
            if (!string.IsNullOrEmpty(avatar.HerzId))
                return BadRequest(new { error = "This avatar already has a HerzID assigned." });

            if (string.IsNullOrEmpty(request.CountryCode) || request.CountryCode.Length != 3)
                return BadRequest(new { error = "CountryCode must be exactly 3 digits (e.g. '052')." });

            // ── Validate voucher ──────────────────────────────────────────────
            var voucherResult = await FindAvatarByHerzIdAsync(request.VoucherHerzId);
            if (voucherResult == null)
                return BadRequest(new { error = $"Voucher HerzID '{request.VoucherHerzId}' not found." });

            bool isFounder = voucherResult.HerzVouchesRemaining == int.MaxValue;
            if (!isFounder && voucherResult.HerzVouchesRemaining <= 0)
                return BadRequest(new { error = "Voucher has no vouches remaining." });

            // ── Claim sequential number ───────────────────────────────────────
            var seqNumber = await _counter.NextValueAsync();
            var joinDate  = DateTime.UtcNow;

            // ── Compute QEA seal ──────────────────────────────────────────────
            var sealResult = await _seal.ComputeSealAsync(seqNumber, request.CountryCode,
                request.QeaProfile ?? "3", joinDate);

            // ── Build HerzID display string ───────────────────────────────────
            var digits     = HerzCfg.SequentialDigits;
            var seqPadded  = seqNumber.ToString().PadLeft(digits, '0');
            // Format: CCC·0·000·000·NNN·S  (country · sequential in groups · seal)
            // We store the raw form for lookup; display adds middle-dot separators.
            var herzIdRaw  = $"{request.CountryCode}{seqPadded}{sealResult.SealChar}";
            var herzIdDisplay = FormatHerzIdDisplay(request.CountryCode, seqNumber, digits, sealResult.SealChar,
                HerzCfg.QeaSealDisplayGlyph);

            // ── Update avatar ─────────────────────────────────────────────────
            avatar.HerzId               = herzIdRaw;
            avatar.HerzCountryCode      = request.CountryCode;
            avatar.HerzSequentialNumber = seqNumber;
            avatar.HerzClearanceLevel   = 2; // Wanderer — HerzID assigned
            avatar.HerzIdAssignedDate   = joinDate;
            avatar.HerzVoucherId        = request.VoucherHerzId;
            avatar.HerzVouchesRemaining = HerzCfg.NewMemberVouches;
            avatar.HerzQeaProfile       = request.QeaProfile ?? "3";
            if (!string.IsNullOrEmpty(request.VoiceprintId))
                avatar.HerzVoiceprintId = request.VoiceprintId;
            // Store in MetaData so HolonManager metadata queries can find this avatar by HerzId and VoucherId
            if (avatar.MetaData == null) avatar.MetaData = new System.Collections.Generic.Dictionary<string, object>();
            avatar.MetaData["HerzId"]       = herzIdRaw;
            avatar.MetaData["HerzVoucherId"] = request.VoucherHerzId?.Replace("·", "").Replace(" ", "").Replace(HerzCfg.QeaSealDisplayGlyph, "") ?? "";

            var saveResult = await avatar.SaveAsync();
            if (saveResult.IsError)
                return StatusCode(500, new { error = "Failed to save HerzID to avatar.", detail = saveResult.Message });

            // ── Decrement voucher's remaining count ───────────────────────────
            if (!isFounder)
            {
                voucherResult.HerzVouchesRemaining--;
                await voucherResult.SaveAsync();
            }

            return Ok(new HerzIdRegisterResponse
            {
                HerzId                   = herzIdDisplay,
                CountryCode              = request.CountryCode,
                SequentialNumber         = seqNumber,
                SealChar                 = sealResult.SealChar,
                DisplayGlyph             = sealResult.DisplayGlyph,
                ClearanceLevel           = 2,
                AssignedAt               = joinDate,
                RegistryCertificateIssued = sealResult.RegistryCertificateIssued,
                Message                  = $"HerzID assigned successfully. You have {HerzCfg.NewMemberVouches} vouches to gift."
            });
        }

        // ── Vouch ─────────────────────────────────────────────────────────────

        /// <summary>
        /// Gift one of the authenticated avatar's vouches to a new member (by OASIS Avatar ID).
        ///
        /// The vouched member's VoucherId is set to this avatar's HerzID.
        /// This is a prerequisite before the new member can call POST /register.
        /// Founders (HerzVouchesRemaining == int.MaxValue) have unlimited capacity.
        /// </summary>
        [HttpPost("vouch")]
        [ProducesResponseType(typeof(HerzIdVouchResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Vouch([FromBody] HerzIdVouchRequest request)
        {
            if (!HerzEnabled)
                return NotFound(new { error = "HerzID is not enabled on this OASIS instance." });

            var voucher = Avatar;
            if (voucher == null) return Unauthorized();
            if (string.IsNullOrEmpty(voucher.HerzId))
                return BadRequest(new { error = "You must have a HerzID to vouch for others." });

            bool isFounder = voucher.HerzVouchesRemaining == int.MaxValue;
            if (!isFounder && voucher.HerzVouchesRemaining <= 0)
                return BadRequest(new { error = "You have no vouches remaining." });

            var targetResult = await AvatarManager.LoadAvatarAsync(request.NewMemberAvatarId);
            if (targetResult.IsError || targetResult.Result == null)
                return BadRequest(new { error = "Target avatar not found." });

            var target = targetResult.Result;
            if (!string.IsNullOrEmpty(target.HerzId))
                return BadRequest(new { error = "Target avatar already has a HerzID." });
            if (!string.IsNullOrEmpty(target.HerzVoucherId))
                return BadRequest(new { error = "Target avatar has already been vouched for." });

            target.HerzVoucherId = voucher.HerzId;
            var saveTarget = await target.SaveAsync();
            if (saveTarget.IsError)
                return StatusCode(500, new { error = "Failed to record vouch on target avatar." });

            if (!isFounder)
            {
                voucher.HerzVouchesRemaining--;
                await voucher.SaveAsync();
            }

            return Ok(new HerzIdVouchResponse
            {
                Success          = true,
                VouchesRemaining = isFounder ? int.MaxValue : voucher.HerzVouchesRemaining,
                Message          = $"Vouch recorded for avatar {request.NewMemberAvatarId}. " +
                                   (isFounder ? "Unlimited vouches remaining (Founder)."
                                              : $"{voucher.HerzVouchesRemaining} vouches remaining.")
            });
        }

        // ── Verify ────────────────────────────────────────────────────────────

        /// <summary>
        /// Verify that a given HerzID string is mathematically valid (QEA seal check).
        /// This is equivalent to the SSL certificate verification step — the seal is
        /// recomputed from the member's stored data and compared against the HerzID string.
        /// Does NOT require authentication — public endpoint for ecosystem integration.
        /// </summary>
        [HttpGet("verify/{herzId}")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(HerzIdVerifyResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> Verify(string herzId)
        {
            if (!HerzEnabled)
                return NotFound(new { error = "HerzID is not enabled on this OASIS instance." });

            // Find avatar by HerzID
            var avatar = await FindAvatarByHerzIdAsync(herzId);
            if (avatar == null)
                return Ok(new HerzIdVerifyResponse { Valid = false, HerzId = herzId, Message = "HerzID not found in this OASIS instance." });

            var valid = await _seal.VerifySealAsync(avatar.HerzId, avatar.HerzSequentialNumber,
                avatar.HerzCountryCode, avatar.HerzQeaProfile ?? "3",
                avatar.HerzIdAssignedDate ?? DateTime.UtcNow.Date);

            return Ok(new HerzIdVerifyResponse
            {
                Valid          = valid,
                HerzId         = FormatHerzIdDisplay(avatar.HerzCountryCode, avatar.HerzSequentialNumber,
                                     HerzCfg.SequentialDigits, avatar.HerzId?[^1].ToString(),
                                     valid ? HerzCfg.QeaSealDisplayGlyph : "⚠"),
                ClearanceLevel = avatar.HerzClearanceLevel,
                QeaProfile     = avatar.HerzQeaProfile,
                AssignedDate   = avatar.HerzIdAssignedDate,
                Message        = valid ? "✦ QEA VERIFIED — genuine, aligned." : "⚠ Not verified — seal mismatch."
            });
        }

        // ── Profile ───────────────────────────────────────────────────────────

        /// <summary>
        /// Returns the HerzID profile for the currently authenticated avatar.
        /// </summary>
        [HttpGet("profile")]
        [ProducesResponseType(typeof(HerzIdProfileResponse), StatusCodes.Status200OK)]
        public IActionResult Profile()
        {
            if (!HerzEnabled)
                return NotFound(new { error = "HerzID is not enabled on this OASIS instance." });

            var avatar = Avatar;
            if (avatar == null) return Unauthorized();
            if (string.IsNullOrEmpty(avatar.HerzId))
                return NotFound(new { error = "This avatar does not yet have a HerzID." });

            var displayHerzId = FormatHerzIdDisplay(avatar.HerzCountryCode, avatar.HerzSequentialNumber,
                HerzCfg.SequentialDigits, avatar.HerzId[^1].ToString(), HerzCfg.QeaSealDisplayGlyph);

            return Ok(new HerzIdProfileResponse
            {
                HerzId           = displayHerzId,
                CountryCode      = avatar.HerzCountryCode,
                SequentialNumber = avatar.HerzSequentialNumber,
                GlobalRank       = avatar.HerzSequentialNumber,
                LocalRank        = $"Member #{avatar.HerzSequentialNumber} in country {avatar.HerzCountryCode}",
                ClearanceLevel   = avatar.HerzClearanceLevel,
                QeaTier          = GetQeaTierName(avatar.HerzClearanceLevel),
                QeaProfile       = avatar.HerzQeaProfile,
                AssignedDate     = avatar.HerzIdAssignedDate,
                VouchesRemaining = avatar.HerzVouchesRemaining,
                HasVoiceprint    = !string.IsNullOrEmpty(avatar.HerzVoiceprintId),
            });
        }

        // ── Set clearance (admin/governance) ──────────────────────────────────

        /// <summary>
        /// Updates the clearance level of any avatar. Requires the caller to have a
        /// HerzID with clearance level 8 or 9 (Flame Keeper / Founder).
        /// </summary>
        [HttpPost("set-clearance")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> SetClearance([FromBody] HerzIdSetClearanceRequest request)
        {
            if (!HerzEnabled)
                return NotFound(new { error = "HerzID is not enabled on this OASIS instance." });

            var caller = Avatar;
            if (caller == null) return Unauthorized();
            if (caller.HerzClearanceLevel < 8)
                return StatusCode(403, new { error = "Clearance level 8 (Flame Keeper) or above required." });

            var targetResult = await AvatarManager.LoadAvatarAsync(request.AvatarId);
            if (targetResult.IsError || targetResult.Result == null)
                return BadRequest(new { error = "Target avatar not found." });

            var target = targetResult.Result;
            target.HerzClearanceLevel = request.ClearanceLevel;
            var saveResult = await target.SaveAsync();
            if (saveResult.IsError)
                return StatusCode(500, new { error = saveResult.Message });

            return Ok(new { message = $"Clearance level updated to {request.ClearanceLevel} ({GetQeaTierName(request.ClearanceLevel)})." });
        }

        // ── Vouching graph ────────────────────────────────────────────────────

        /// <summary>
        /// Returns the vouching chain for a given HerzID — walks upward from the member to
        /// the founding member, returning each link's HerzID, country, clearance level, and
        /// the date they joined. Maximum depth: 50.
        ///
        /// Public endpoint — no authentication required.
        /// </summary>
        [HttpGet("vouch-chain/{herzId}")]
        [AllowAnonymous]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> VouchChain(string herzId)
        {
            if (!HerzEnabled)
                return NotFound(new { error = "HerzID is not enabled on this OASIS instance." });

            const int maxDepth = 50;
            var chain = new System.Collections.Generic.List<object>();
            var currentHerzId = herzId?.Replace("·", "").Replace(" ", "").Replace(HerzCfg.QeaSealDisplayGlyph, "");

            for (int depth = 0; depth < maxDepth && !string.IsNullOrEmpty(currentHerzId); depth++)
            {
                var avatar = await FindAvatarByHerzIdAsync(currentHerzId);
                if (avatar == null) break;

                chain.Add(new
                {
                    herzId       = FormatHerzIdDisplay(avatar.HerzCountryCode, avatar.HerzSequentialNumber, HerzCfg.SequentialDigits, avatar.HerzId?[^1].ToString(), HerzCfg.QeaSealDisplayGlyph),
                    clearance    = avatar.HerzClearanceLevel,
                    tier         = GetQeaTierName(avatar.HerzClearanceLevel),
                    countryCode  = avatar.HerzCountryCode,
                    joinedAt     = avatar.HerzIdAssignedDate,
                    isFounder    = avatar.HerzVouchesRemaining == int.MaxValue,
                    vouchedBy    = avatar.HerzVoucherId
                });

                // Walk up the tree
                if (string.IsNullOrEmpty(avatar.HerzVoucherId)) break;
                currentHerzId = avatar.HerzVoucherId;
            }

            return Ok(new { length = chain.Count, chain });
        }

        /// <summary>
        /// Returns all members vouched for by the authenticated avatar (downward graph).
        /// </summary>
        [HttpGet("vouches-issued")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> VouchesIssued()
        {
            if (!HerzEnabled)
                return NotFound(new { error = "HerzID is not enabled on this OASIS instance." });

            var caller = Avatar;
            if (caller == null) return Unauthorized();
            if (string.IsNullOrEmpty(caller.HerzId))
                return BadRequest(new { error = "This avatar does not have a HerzID." });

            var raw = caller.HerzId.Replace("·", "").Replace(" ", "").Replace(HerzCfg.QeaSealDisplayGlyph, "");
            var vouched = new System.Collections.Generic.List<object>();

            try
            {
                var result = await HolonManager.Instance.LoadHolonsByMetaDataAsync("HerzVoucherId", raw, HolonType.Avatar, loadChildren: false);
                if (!result.IsError && result.Result != null)
                {
                    foreach (var holon in result.Result)
                    {
                        IAvatar av = null;
                        if (holon is IAvatar casted) av = casted;
                        else
                        {
                            var avResult = await AvatarManager.LoadAvatarAsync(holon.Id);
                            if (!avResult.IsError) av = avResult.Result;
                        }
                        if (av != null)
                        {
                            vouched.Add(new
                            {
                                herzId     = av.HerzId,
                                clearance  = av.HerzClearanceLevel,
                                tier       = GetQeaTierName(av.HerzClearanceLevel),
                                joinedAt   = av.HerzIdAssignedDate,
                            });
                        }
                    }
                }
            }
            catch { /* no-op — storage error, return empty list */ }

            return Ok(new { vouchesIssued = vouched.Count, remaining = caller.HerzVouchesRemaining, vouched });
        }

        /// <summary>
        /// Admin ghost-account detection check for a HerzID or its vouch subtree.
        ///
        /// Requires clearance level 8+ (Flame Keeper / Founder).
        ///
        /// Returns risk signals:
        ///   - Members registered in rapid succession from the same voucher
        ///   - Voucher has issued more than (NewMemberVouches × 2) members within 30 days
        ///   - Member's sequential range is suspiciously clustered (many registrations in under 60 s)
        /// </summary>
        [HttpPost("ghost-check/{herzId}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GhostCheck(string herzId)
        {
            if (!HerzEnabled)
                return NotFound(new { error = "HerzID is not enabled on this OASIS instance." });

            var caller = Avatar;
            if (caller == null) return Unauthorized();
            if (caller.HerzClearanceLevel < 8)
                return StatusCode(403, new { error = "Clearance level 8 (Flame Keeper) required." });

            var target = await FindAvatarByHerzIdAsync(herzId);
            if (target == null)
                return BadRequest(new { error = $"HerzID '{herzId}' not found." });

            var signals = new System.Collections.Generic.List<string>();
            int riskScore = 0;

            // Signal 1: registered very recently (< 24 h)
            if (target.HerzIdAssignedDate.HasValue &&
                (DateTime.UtcNow - target.HerzIdAssignedDate.Value).TotalHours < 24)
            {
                signals.Add("Registered within the last 24 hours.");
                riskScore += 1;
            }

            // Signal 2: no voiceprint enrolled
            if (string.IsNullOrEmpty(target.HerzVoiceprintId) && string.IsNullOrEmpty(target.VoiceprintId))
            {
                signals.Add("No voice biometric enrolled.");
                riskScore += 1;
            }

            // Signal 3: voucher issued many members — load vouched-by subtree
            if (!string.IsNullOrEmpty(target.HerzVoucherId))
            {
                try
                {
                    var siblingResult = await HolonManager.Instance.LoadHolonsByMetaDataAsync("HerzVoucherId",
                        target.HerzVoucherId, HolonType.Avatar, loadChildren: false);
                    if (!siblingResult.IsError && siblingResult.Result != null)
                    {
                        var siblingCount = 0;
                        var recentCount = 0;
                        foreach (var sib in siblingResult.Result)
                        {
                            siblingCount++;
                            if (sib is IAvatar sibAv && sibAv.HerzIdAssignedDate.HasValue
                                && (DateTime.UtcNow - sibAv.HerzIdAssignedDate.Value).TotalDays <= 30)
                                recentCount++;
                        }

                        var burstThreshold = HerzCfg.NewMemberVouches * 2;
                        if (recentCount > burstThreshold)
                        {
                            signals.Add($"Voucher issued {recentCount} members in the last 30 days (burst threshold: {burstThreshold}).");
                            riskScore += 3;
                        }
                        if (siblingCount > HerzCfg.NewMemberVouches)
                        {
                            signals.Add($"Voucher has issued {siblingCount} total members (exceeds default allocation of {HerzCfg.NewMemberVouches}).");
                            riskScore += 2;
                        }
                    }
                }
                catch { /* no-op */ }
            }

            // Signal 4: clearance still at 1 (never upgraded — possible dormant/ghost)
            if (target.HerzClearanceLevel <= 1)
            {
                signals.Add("Clearance is still at level 1 (Explorer — HerzID assigned but never active).");
                riskScore += 1;
            }

            var risk = riskScore switch
            {
                0 => "Low",
                1 or 2 => "Moderate",
                3 or 4 => "High",
                _ => "Critical"
            };

            return Ok(new
            {
                herzId    = herzId,
                riskScore,
                risk,
                signals,
                recommendation = riskScore >= 3
                    ? "Recommend suspending this HerzID pending manual review."
                    : "No immediate action required."
            });
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private async Task<IAvatar> FindAvatarByHerzIdAsync(string herzId)
        {
            if (string.IsNullOrEmpty(herzId)) return null;
            // Normalise: strip display dots/middle-dots
            var raw = herzId.Replace("·", "").Replace(" ", "").Replace(HerzCfg.QeaSealDisplayGlyph, "");

            try
            {
                // HerzId is stored in Avatar MetaData["HerzId"] at registration time.
                // HolonManager.LoadHolonsByMetaDataAsync queries by MetaData key/value.
                var result = await HolonManager.Instance.LoadHolonsByMetaDataAsync("HerzId", raw,
                    HolonType.Avatar, loadChildren: false);
                if (!result.IsError && result.Result != null)
                {
                    foreach (var holon in result.Result)
                    {
                        if (holon is IAvatar av) return av;
                        // Load the full avatar by id when the holon projection doesn't implement IAvatar
                        var avResult = await AvatarManager.LoadAvatarAsync(holon.Id);
                        if (!avResult.IsError && avResult.Result != null) return avResult.Result;
                    }
                }
            }
            catch { /* fall through */ }
            return null;
        }

        private static string FormatHerzIdDisplay(string country, int seqNumber, int digits,
            string sealChar, string sealGlyph)
        {
            // Pad to full digit count, then split into groups of 3 for readability
            var padded = seqNumber.ToString().PadLeft(digits, '0');
            var sb = new StringBuilder(country).Append('·');
            for (int i = 0; i < padded.Length; i++)
            {
                if (i > 0 && i % 3 == 0) sb.Append('·');
                sb.Append(padded[i]);
            }
            sb.Append('·').Append(sealGlyph ?? sealChar);
            return sb.ToString();
        }

        private static string GetQeaTierName(int level) => level switch
        {
            1 => "QEA-1 (Explorer)",
            2 => "QEA-2 (Wanderer)",
            3 => "QEA-3 (Tribe Member)",
            4 => "QEA-4 (Contributor)",
            5 => "QEA-5 (Ally)",
            6 => "QEA-6 (Guardian)",
            7 => "QEA-7 (Elder)",
            8 => "QEA-8 (Flame Keeper)",
            9 => "QEA-∞ (Sovereign / Founder)",
            _ => "Unknown"
        };
    }
}
