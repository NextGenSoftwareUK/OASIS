using NextGenSoftware.OASIS.API.Core.Interfaces;

namespace NextGenSoftware.OASIS.API.Core.Helpers
{
    public static class HolonMapper
    {
        /// <summary>
        /// Copies all scalar holon properties from source onto target in-place.
        /// Used after Load/Save to refresh the in-memory holon with data returned by the provider.
        /// </summary>
        public static void SetProperties(IHolon target, IHolon source)
        {
            if (target == null || source == null) return;

            target.Id = source.Id;
            target.Name = source.Name;
            target.Description = source.Description;
            target.HolonType = source.HolonType;
            target.IsActive = source.IsActive;
            target.IsChanged = source.IsChanged;
            target.IsNewHolon = source.IsNewHolon;
            target.IsSaving = source.IsSaving;
            target.MetaData = source.MetaData;
            target.ProviderMetaData = source.ProviderMetaData;
            target.ProviderUniqueStorageKey = source.ProviderUniqueStorageKey;
            target.CreatedByAvatar = source.CreatedByAvatar;
            target.CreatedByAvatarId = source.CreatedByAvatarId;
            target.CreatedDate = source.CreatedDate;
            target.CreatedOASISType = source.CreatedOASISType;
            target.CreatedProviderType = source.CreatedProviderType;
            target.ModifiedByAvatar = source.ModifiedByAvatar;
            target.ModifiedByAvatarId = source.ModifiedByAvatarId;
            target.ModifiedDate = source.ModifiedDate;
            target.DeletedByAvatar = source.DeletedByAvatar;
            target.DeletedByAvatarId = source.DeletedByAvatarId;
            target.DeletedDate = source.DeletedDate;
            target.InstanceSavedOnProviderType = source.InstanceSavedOnProviderType;
            target.Original = source.Original;
            target.PreviousVersionId = source.PreviousVersionId;
            target.PreviousVersionProviderUniqueStorageKey = source.PreviousVersionProviderUniqueStorageKey;
            target.Version = source.Version;
            target.VersionId = source.VersionId;
        }
    }
}
