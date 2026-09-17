using System;
using System.ComponentModel.DataAnnotations;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Models.HerzId
{
    // ── Registration ──────────────────────────────────────────────────────────

    public class HerzIdRegisterRequest
    {
        /// <summary>ISO telephone country code (e.g. "052" for Mexico, "001" for USA).</summary>
        [Required] public string CountryCode { get; set; }

        /// <summary>
        /// QEA frequency profile — the sacred number sequence chosen or assigned during registration
        /// (e.g. "369", "369-144", "369-144-999"). Determines the member's QEA tier.
        /// </summary>
        [Required] public string QeaProfile { get; set; }

        /// <summary>HerzID of the existing verified member vouching for this avatar.</summary>
        [Required] public string VoucherHerzId { get; set; }

        /// <summary>
        /// Opaque voice biometric profile ID returned by the caller after they have enrolled the
        /// member's voice declaration audio with the voice provider (e.g. Azure Speaker Recognition).
        /// Pass empty string to skip voice biometrics in Phase 1 / testing.
        /// </summary>
        public string VoiceprintId { get; set; } = "";
    }

    public class HerzIdRegisterResponse
    {
        public string HerzId { get; set; }
        public string CountryCode { get; set; }
        public int SequentialNumber { get; set; }
        public string SealChar { get; set; }
        public string DisplayGlyph { get; set; }
        public int ClearanceLevel { get; set; }
        public DateTime AssignedAt { get; set; }
        public bool RegistryCertificateIssued { get; set; }
        public string Message { get; set; }
    }

    // ── Vouching ──────────────────────────────────────────────────────────────

    public class HerzIdVouchRequest
    {
        /// <summary>Avatar ID (GUID) of the new member being vouched for.</summary>
        [Required] public Guid NewMemberAvatarId { get; set; }
    }

    public class HerzIdVouchResponse
    {
        public bool Success { get; set; }
        public int VouchesRemaining { get; set; }
        public string Message { get; set; }
    }

    // ── Verification ──────────────────────────────────────────────────────────

    public class HerzIdVerifyResponse
    {
        public bool Valid { get; set; }
        public string HerzId { get; set; }
        public int ClearanceLevel { get; set; }
        public string QeaProfile { get; set; }
        public DateTime? AssignedDate { get; set; }
        public string Message { get; set; }
    }

    // ── Profile ───────────────────────────────────────────────────────────────

    public class HerzIdProfileResponse
    {
        public string HerzId { get; set; }
        public string CountryCode { get; set; }
        public int SequentialNumber { get; set; }
        public int GlobalRank { get; set; }
        public string LocalRank { get; set; }
        public int ClearanceLevel { get; set; }
        public string QeaTier { get; set; }
        public string QeaProfile { get; set; }
        public DateTime? AssignedDate { get; set; }
        public int VouchesRemaining { get; set; }
        public bool HasVoiceprint { get; set; }
    }

    // ── Clearance update ──────────────────────────────────────────────────────

    public class HerzIdSetClearanceRequest
    {
        [Required] public Guid AvatarId { get; set; }
        [Range(1, 9)] public int ClearanceLevel { get; set; }
    }
}
