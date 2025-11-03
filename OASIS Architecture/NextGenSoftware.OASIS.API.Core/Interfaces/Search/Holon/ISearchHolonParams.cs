
namespace NextGenSoftware.OASIS.API.Core.Interfaces.Search.Holon
{
    public interface ISearchHolonParams : ISearchParamsBase
    {
        //From Holon
        bool Children { get; set; }
        bool Nodes { get; set; }

        //From HolonBase
        bool HolonType { get; set; }
        bool Name { get; set; }
        bool Description { get; set; }
        bool ProviderUniqueStorageKey { get; set; }
        bool ProviderMetaData { get; set; }
        bool MetaData { get; set; }
        string MetaDataKey { get; set; }
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
    }
}