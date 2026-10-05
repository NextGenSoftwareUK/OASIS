
using NextGenSoftware.OASIS.API.Core.Interfaces.Search;
using NextGenSoftware.OASIS.API.Core.Interfaces.Search.Holon;

namespace NextGenSoftware.OASIS.API.Core.Interfaces
{
    public class SearchHolonParams : SearchParamsBase, ISearchHolonParams
    {
        //From Holon
        public bool Children { get; set; }
        public bool Nodes { get; set; }

        //From HolonBase
        public bool HolonType { get; set; }
        public bool Name { get; set; }
        public bool Description { get; set; }
        public bool ProviderUniqueStorageKey { get; set; }
        public bool ProviderMetaData { get; set; }
        public bool MetaData { get; set; }
        public string MetaDataKey { get; set; }
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
    }
}