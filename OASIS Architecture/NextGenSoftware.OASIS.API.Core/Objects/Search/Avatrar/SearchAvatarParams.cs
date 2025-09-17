using NextGenSoftware.OASIS.API.Core.Interfaces.Search;
using NextGenSoftware.OASIS.API.Core.Interfaces.Search.Avatar;

namespace NextGenSoftware.OASIS.API.Core.Objects.Search.Avatrar
{
    public class SearchAvatarParams : SearchParamsBase, ISearchAvatarParams
    {
        //From IAvatar
        public ISearchAvatarWalletParams SearchAvatarWalletParams { get; set; }
        public bool ProviderUsername { get; set; }
        public bool AvatarId { get; set; }
        public bool Title { get; set; }
        public bool FirstName { get; set; }
        public bool LastName { get; set; }
        public bool Username { get; set; }
        public bool Email { get; set; }
        public bool AvatarType { get; set; }
        public bool AcceptTerms { get; set; }
        public bool IsVerified { get; }
        public bool VerifiedDate { get; set; }
        public bool LastBeamedIn { get; set; }
        public bool LastBeamedOut { get; set; }
        public bool IsBeamedIn { get; set; }

        //From HolonBase
        public bool ProviderUniqueStorageKey { get; set; }
        public bool ProviderMetaData { get; set; }
        public bool MetaData { get; set; }
        public bool Version { get; set; }
        public bool VersionId { get; set; }
        public bool PreviousVersionId { get; set; }
        public bool PreviousVersionProviderUniqueStorageKey { get; set; }
        public bool IsActive { get; set; }
        public bool CreatedByAvatarId { get; set; }
        public bool CreatedDate { get; set; }
        public bool ModifiedByAvatarId { get; set; }
        public bool ModifiedDate { get; set; }
        public bool DeletedByAvatarId { get; set; }
        public bool DeletedDate { get; set; }
        public bool CreatedProviderType { get; set; }
        public bool CreatedOASISType { get; set; }


        //public bool SearchFirstName { get; set; }
        //public bool SearchLastName { get; set; }
        //public bool SearchUsername { get; set; }
        //public bool SearchAddress { get; set; }

        //TODO: Add rest of Avatar fields ASAP!
    }
}