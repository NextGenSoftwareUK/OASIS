using System;
using System.Collections.Generic;
using NextGenSoftware.Holochain.HoloNET.ORM.Interfaces;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Objects;

namespace NextGenSoftware.OASIS.API.Providers.HoloOASIS
{
    public interface IHcAvatar : IHoloNETAuditEntryBase //: IHcObject
    {
        #region IAvatar Properties

        Guid Id { get; set; }
        string Username { get; set; }
        string Password { get; set; }
        string Email { get; set; }
        string Title { get; set; }
        string FirstName { get; set; }
        string LastName { get; set; }
        EnumValue<AvatarType> AvatarType { get; set; }
        bool AcceptTerms { get; set; }
        bool IsVerified { get; }
        string JwtToken { get; set; }
        DateTime? PasswordReset { get; set; }
        string RefreshToken { get; set; }
        List<RefreshToken> RefreshTokens { get; set; }
        string ResetToken { get; set; }
        DateTime? ResetTokenExpires { get; set; }
        string VerificationToken { get; set; }
        DateTime? Verified { get; set; }
        DateTime? LastBeamedIn { get; set; }
        DateTime? LastBeamedOut { get; set; }
        bool IsBeamedIn { get; set; }

        #endregion

        #region IHolonBase Properties

        string Name { get; set; }
        string Description { get; set; }
        HolonType HolonType { get; set; }
        string ProviderUniqueStorageKey { get; set; }
        Dictionary<ProviderType, string> PreviousVersionProviderUniqueStorageKey { get; set; }
        public Dictionary<ProviderType, List<IProviderWallet>> ProviderWallets { get; set; }
        public Dictionary<ProviderType, string> ProviderUsername { get; set; }
        Dictionary<ProviderType, Dictionary<string, string>> ProviderMetaData { get; set; }
        Dictionary<string, string> MetaData { get; set; }
        int Version { get; set; }
        Guid VersionId { get; set; }
        Guid PreviousVersionId { get; set; }
        bool IsActive { get; set; }
        string CreatedBy { get; set; }
        DateTime CreatedDate { get; set; }
        string ModifiedBy { get; set; }
        DateTime ModifiedDate { get; set; }
        string DeletedBy { get; set; }
        DateTime DeletedDate { get; set; }
        EnumValue<ProviderType> CreatedProviderType { get; set; }
        EnumValue<OASISType> CreatedOASISType { get; set; }

        #endregion
    }
}