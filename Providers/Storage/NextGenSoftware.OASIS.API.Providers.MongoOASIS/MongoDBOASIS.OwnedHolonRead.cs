using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.MongoDBOASIS;

/// <summary>Owner-scoped holon listing filtered by CreatedByAvatarId in MongoDB. The interface default
/// lists the whole collection and filters afterwards, so one ambiguous identity anywhere in the store
/// failed every owner's listing (the sandbox sweep, for example). Public-inclusive and graph or
/// historical reads keep the default visibility projection.</summary>
public partial class MongoDBOASIS : IOASISStorageProvider
{
    public async Task<OASISResult<IEnumerable<IHolon>>> LoadAllHolonsAsync(Guid avatarId, bool includePublic = false, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
    {
        if (avatarId == Guid.Empty || includePublic || version > 0 || loadChildren && loadChildrenFromProvider)
            return IOASISStorageProvider.ProjectVisibleHolons(await LoadAllHolonsAsync(type, loadChildren, recursive, maxChildDepth,
                curentChildDepth, continueOnError, loadChildrenFromProvider, version).ConfigureAwait(false), avatarId, includePublic);
        return IOASISStorageProvider.ProjectVisibleHolons(await ReadHolonCollectionGraphAsync(type, null, null, null, MetaKeyValuePairMatchMode.All, loadChildren,
            recursive, maxChildDepth, curentChildDepth, loadChildrenFromProvider, version, false, avatarId).ConfigureAwait(false), avatarId, includePublic);
    }

    public OASISResult<IEnumerable<IHolon>> LoadAllHolons(Guid avatarId, bool includePublic = false, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
    {
        if (avatarId == Guid.Empty || includePublic || version > 0 || loadChildren && loadChildrenFromProvider)
            return IOASISStorageProvider.ProjectVisibleHolons(LoadAllHolons(type, loadChildren, recursive, maxChildDepth,
                curentChildDepth, continueOnError, loadChildrenFromProvider, version), avatarId, includePublic);
        return IOASISStorageProvider.ProjectVisibleHolons(ReadHolonCollectionGraphAsync(type, null, null, null, MetaKeyValuePairMatchMode.All, loadChildren,
            recursive, maxChildDepth, curentChildDepth, loadChildrenFromProvider, version, true, avatarId).GetAwaiter().GetResult(), avatarId, includePublic);
    }
}
