#nullable enable
using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.AvatarProfile;

public sealed record AvatarProfileChangeSet(bool AvatarChanged, bool DetailChanged);

/// <summary>Applies only explicitly supplied profile fields to an owned, matching pair of account records.</summary>
public static class AvatarProfileChanges
{
    public static OASISResult<AvatarProfileChangeSet> Apply(AvatarProfilePatch patch, IAvatar avatar, IAvatarDetail detail)
    {
        if (patch == null || avatar == null || detail == null)
            return Error("AVATAR_PROFILE_INPUT_REQUIRED", "Profile changes and both account records are required.");
        if (avatar.Id == Guid.Empty || avatar.Id != detail.Id)
            return Error("AVATAR_PROFILE_ID_MISMATCH", "Profile records must belong to the same avatar.");
        var invalid = patch.Validate(new ValidationContext(patch)).FirstOrDefault();
        if (invalid != null) return Error("AVATAR_PROFILE_INVALID", invalid.ErrorMessage ?? "Invalid profile changes.");

        var avatarChanged = false;
        var detailChanged = false;
        if (patch.IsSpecified(AvatarProfileField.Title))
        {
            var value = patch.Title ?? string.Empty;
            avatarChanged |= !string.Equals(avatar.Title, value, StringComparison.Ordinal);
            avatar.Title = value;
        }
        if (patch.IsSpecified(AvatarProfileField.FirstName))
        {
            var value = patch.FirstName ?? string.Empty;
            avatarChanged |= !string.Equals(avatar.FirstName, value, StringComparison.Ordinal);
            avatar.FirstName = value;
        }
        if (patch.IsSpecified(AvatarProfileField.LastName))
        {
            var value = patch.LastName ?? string.Empty;
            avatarChanged |= !string.Equals(avatar.LastName, value, StringComparison.Ordinal);
            avatar.LastName = value;
        }
        if (patch.IsSpecified(AvatarProfileField.Username))
        {
            avatarChanged |= !string.Equals(avatar.Username, patch.Username, StringComparison.Ordinal);
            detailChanged |= !string.Equals(detail.Username, patch.Username, StringComparison.Ordinal);
            avatar.Username = patch.Username!;
            detail.Username = patch.Username!;
        }
        if (patch.IsSpecified(AvatarProfileField.Email))
        {
            avatarChanged |= !string.Equals(avatar.Email, patch.Email, StringComparison.Ordinal);
            detailChanged |= !string.Equals(detail.Email, patch.Email, StringComparison.Ordinal);
            avatar.Email = patch.Email!;
            detail.Email = patch.Email!;
        }
        if (patch.IsSpecified(AvatarProfileField.Address))
        {
            var value = patch.Address ?? string.Empty;
            detailChanged |= !string.Equals(detail.Address, value, StringComparison.Ordinal);
            detail.Address = value;
        }
        // Preparation is not persistence: callers must check every required storage result before reporting success.
        return new OASISResult<AvatarProfileChangeSet>
        {
            Result = new AvatarProfileChangeSet(avatarChanged, detailChanged),
            Message = "Profile changes prepared."
        };
    }

    private static OASISResult<AvatarProfileChangeSet> Error(string code, string message)
        => new() { IsError = true, ErrorCount = 1, ErrorCode = code, Message = message };
}
