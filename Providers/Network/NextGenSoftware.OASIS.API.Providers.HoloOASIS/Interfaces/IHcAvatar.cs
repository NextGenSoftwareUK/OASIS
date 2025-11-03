using System;
using System.Collections.Generic;
using NextGenSoftware.Utilities;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.Holochain.HoloNET.ORM.Interfaces;

namespace NextGenSoftware.OASIS.API.Providers.HoloOASIS
{
    public interface IHcAvatar : IHoloNETAuditEntryBase
    {
        #region IAvatar Properties

        Dictionary<ProviderType, List<IProviderWallet>> ProviderWallets { get; set; }
        Dictionary<ProviderType, string> ProviderUsername { get; set; }
        Guid Id { get; set; }
        string Username { get; set; }
        string Password { get; set; }
        string Email { get; set; }
        string Title { get; set; }
        string FirstName { get; set; }
        string LastName { get; set; }
        EnumValue<AvatarType> AvatarType { get; set; }
        bool AcceptTerms { get; set; }
        //bool IsVerified { get; set; } //Not needed, is computed.
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

        IList<IHolon> Children { get; set; } //Allows any holon to add any number of custom child holons to it.
        //IReadOnlyCollection<IHolon> AllChildren { get; set; } //Readonly collection of all the total children including all the zomes, celestialbodies, celestialspaces, moons, holons, planets, stars etc belong to the holon.
        //Guid CreatedByAvatarId { get; set; }
        //DateTime CreatedDate { get; set; }
        EnumValue<OASISType> CreatedOASISType { get; set; }
        EnumValue<ProviderType> CreatedProviderType { get; set; }
        string CustomKey { get; set; }
        //Guid DeletedByAvatarId { get; set; }
        //DateTime DeletedDate { get; set; }
        string Description { get; set; }
        HolonType HolonType { get; set; }
        EnumValue<ProviderType> InstanceSavedOnProviderType { get; set; }
        bool IsActive { get; set; }
        //bool IsChanged { get; set; }
        //bool IsNewHolon { get; set; }
        //bool IsSaving { get; set; }
        Dictionary<string, object> MetaData { get; set; }
        //Guid ModifiedByAvatarId { get; set; }
        //DateTime ModifiedDate { get; set; }
        string Name { get; set; }
        //IHolon Original { get; set; }
        //IHolon ParentHolon { get; set; }
        Guid ParentHolonId { get; set; }
        Guid PreviousVersionId { get; set; }
        Dictionary<ProviderType, string> PreviousVersionProviderUniqueStorageKey { get; set; }
        Dictionary<ProviderType, Dictionary<string, string>> ProviderMetaData { get; set; }
        Dictionary<ProviderType, string> ProviderUniqueStorageKey { get; set; }
        int Version { get; set; }
        Guid VersionId { get; set; }
        string ChildIdListCache { get; set; } //This will store the list of id's for the direct childen of this holon.
        string AllChildIdListCache { get; set; } //This will store the list of id's for the ALL the childen of this holon (including all sub-childen).

        #endregion
    }
}