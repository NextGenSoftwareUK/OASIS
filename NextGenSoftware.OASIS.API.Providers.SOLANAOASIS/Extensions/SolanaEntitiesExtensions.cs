namespace NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.Extensions;

public static class SolanaEntitiesExtensions
{
    public static IAvatarDetail GetAvatarDetail(this SolanaAvatarDetailDto avatarDetailDto)
    {
        if (avatarDetailDto == null)
            throw new ArgumentNullException(nameof(avatarDetailDto));
        return new AvatarDetail()
        {
            Id = avatarDetailDto.Id,
            Address = avatarDetailDto.Address,
            Mobile = avatarDetailDto.Mobile,
            Karma = avatarDetailDto.Karma,
            XP = avatarDetailDto.Xp,
            Version = avatarDetailDto.Version,
            IsActive = avatarDetailDto.IsDeleted,
            PreviousVersionId = avatarDetailDto.PreviousVersionId
        };
    }

    public static IAvatar GetAvatar(this SolanaAvatarDto avatarDto)
    {
        if (avatarDto == null)
            throw new ArgumentNullException(nameof(avatarDto));
        return new Avatar()
        {
            Id = avatarDto.Id,
            AvatarId = avatarDto.AvatarId,
            Email = avatarDto.Email,
            Password = avatarDto.Password,
            Username = avatarDto.UserName,
            Version = avatarDto.Version,
            IsActive = avatarDto.IsDeleted,
            PreviousVersionId = avatarDto.PreviousVersionId
        };
    }

    public static IHolon GetHolon(this SolanaHolonDto holonDto)
    {
        if (holonDto == null)
            throw new ArgumentNullException(nameof(holonDto));

        return new Holon()
        {
            Id = holonDto.Id,
            ParentOmniverseId = holonDto.ParentOmniverseId,
            ParentMultiverseId = holonDto.ParentMultiverseId,
            ParentUniverseId = holonDto.ParentUniverseId,
            Version = holonDto.Version,
            IsActive = holonDto.IsDeleted,
            PreviousVersionId = holonDto.PreviousVersionId

            //TODO: This needs finishing!
        };
    }
}