namespace NextGenSoftware.OASIS.API.Core.Interfaces.Search.Avatar
{
    public interface ISearchAvatarParams : ISearchParamsBase
    {        
        //From IAvatar
        ISearchAvatarWalletParams SearchAvatarWalletParams { get; set; }
        bool ProviderUsername { get; set; }
        bool AvatarId { get; set; }
        bool Title { get; set; }
        bool FirstName { get; set; }
        bool LastName { get; set; }
        bool Username { get; set; }
        bool Email { get; set; }
        bool AvatarType { get; set; }
        bool AcceptTerms { get; set; }
        bool IsVerified { get; }
        bool VerifiedDate { get; set; }
        bool LastBeamedIn { get; set; }
        bool LastBeamedOut { get; set; }
        bool IsBeamedIn { get; set; }

        //From HolonBase
        bool ProviderUniqueStorageKey { get; set; }
        bool ProviderMetaData { get; set; }
        bool MetaData { get; set; }
        bool Version { get; set; }
        bool VersionId { get; set; }
        bool PreviousVersionId { get; set; }
        bool PreviousVersionProviderUniqueStorageKey { get; set; }
        bool IsActive { get; set; }
        bool CreatedByAvatarId { get; set; }
        bool CreatedDate { get; set; }
        bool ModifiedByAvatarId { get; set; }
        bool ModifiedDate { get; set; }
        bool DeletedByAvatarId { get; set; }
        bool DeletedDate { get; set; }
        bool CreatedProviderType { get; set; }
        bool CreatedOASISType { get; set; }


        //bool SearchFirstName { get; set; }
        //bool SearchLastName { get; set; }
        //bool SearchUsername { get; set; }
        //bool SearchAddress { get; set; }

        //bool SearchCreatedDate { get;set; }
        //bool SearchCreatedBy { get; set; }
        //bool SearchModifedBy { get; set; }
        //bool SearchModifedDate { get; set; }

        //bool SearchDeletedBy { get; set; }
        //bool SearchDeletedDate { get; set; }

        //TODO: Add rest of Avatar fields ASAP!
    }
}