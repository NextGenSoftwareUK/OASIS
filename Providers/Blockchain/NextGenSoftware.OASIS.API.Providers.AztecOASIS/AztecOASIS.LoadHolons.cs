using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.API.Core.Interfaces.Avatar;
using NextGenSoftware.Utilities;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.DTOs;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.Enums;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Utilities;
using NextGenSoftware.OASIS.API.Core.Interfaces.Search;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.Core.Objects.Avatar;
using NextGenSoftware.OASIS.API.Core.Objects.Search;
using Newtonsoft.Json;
using NextGenSoftware.OASIS.API.Providers.AztecOASIS.Infrastructure.Repositories;
using NextGenSoftware.OASIS.API.Providers.AztecOASIS.Infrastructure.Services.Aztec;
using NextGenSoftware.OASIS.API.Providers.AztecOASIS.Models;
using Nethereum.Signer;
using Nethereum.Hex.HexConvertors.Extensions;
using System.Linq;

namespace NextGenSoftware.OASIS.API.Providers.AztecOASIS
{
    public partial class AztecOASIS
    {
        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByUsernameAsync(string avatarUsername, int version = 0) { await Task.CompletedTask; return NoRecordStore<IAvatarDetail>(); }

        public override OASISResult<IAvatarDetail> LoadAvatarDetailByUsername(string avatarUsername, int version = 0) => NoRecordStore<IAvatarDetail>();

        public override async Task<OASISResult<IEnumerable<IAvatarDetail>>> LoadAllAvatarDetailsAsync(int version = 0) { await Task.CompletedTask; return NoRecordStore<IEnumerable<IAvatarDetail>>(); }

        public override OASISResult<IEnumerable<IAvatarDetail>> LoadAllAvatarDetails(int version = 0) => NoRecordStore<IEnumerable<IAvatarDetail>>();

        public override async Task<OASISResult<IAvatar>> SaveAvatarAsync(IAvatar Avatar) { await Task.CompletedTask; return NoRecordStore<IAvatar>(); }

        public override OASISResult<IAvatar> SaveAvatar(IAvatar Avatar) => NoRecordStore<IAvatar>();

        public override async Task<OASISResult<IAvatarDetail>> SaveAvatarDetailAsync(IAvatarDetail Avatar) { await Task.CompletedTask; return NoRecordStore<IAvatarDetail>(); }

        public override OASISResult<IAvatarDetail> SaveAvatarDetail(IAvatarDetail Avatar) => NoRecordStore<IAvatarDetail>();

        public override async Task<OASISResult<bool>> DeleteAvatarAsync(Guid id, bool softDelete = true) { await Task.CompletedTask; return NoRecordStore<bool>(); }

        public override OASISResult<bool> DeleteAvatar(Guid id, bool softDelete = true) => NoRecordStore<bool>();

        public override async Task<OASISResult<bool>> DeleteAvatarAsync(string providerKey, bool softDelete = true) { await Task.CompletedTask; return NoRecordStore<bool>(); }

        public override OASISResult<bool> DeleteAvatar(string providerKey, bool softDelete = true) => NoRecordStore<bool>();

        public override async Task<OASISResult<bool>> DeleteAvatarByEmailAsync(string avatarEmail, bool softDelete = true) { await Task.CompletedTask; return NoRecordStore<bool>(); }

        public override OASISResult<bool> DeleteAvatarByEmail(string avatarEmail, bool softDelete = true) => NoRecordStore<bool>();

        public override async Task<OASISResult<bool>> DeleteAvatarByUsernameAsync(string avatarUsername, bool softDelete = true) { await Task.CompletedTask; return NoRecordStore<bool>(); }

        public override OASISResult<bool> DeleteAvatarByUsername(string avatarUsername, bool softDelete = true) => NoRecordStore<bool>();

        public override async Task<OASISResult<ISearchResults>> SearchAsync(ISearchParams searchParams, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0) { await Task.CompletedTask; return NoRecordStore<ISearchResults>(); }

        public override OASISResult<ISearchResults> Search(ISearchParams searchParams, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0) => NoRecordStore<ISearchResults>();

        public override async Task<OASISResult<IHolon>> LoadHolonAsync(Guid id, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0) { await Task.CompletedTask; return NoRecordStore<IHolon>(); }

        public override OASISResult<IHolon> LoadHolon(Guid id, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0) => NoRecordStore<IHolon>();

        public override async Task<OASISResult<IHolon>> LoadHolonAsync(string providerKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0) { await Task.CompletedTask; return NoRecordStore<IHolon>(); }

        public override OASISResult<IHolon> LoadHolon(string providerKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0) => NoRecordStore<IHolon>();

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(Guid id, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0) { await Task.CompletedTask; return NoRecordStore<IEnumerable<IHolon>>(); }

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsForParent(Guid id, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0) => NoRecordStore<IEnumerable<IHolon>>();

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(string providerKey, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0) { await Task.CompletedTask; return NoRecordStore<IEnumerable<IHolon>>(); }

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsForParent(string providerKey, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0) => NoRecordStore<IEnumerable<IHolon>>();

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsByMetaDataAsync(string metaKey, string metaValue, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0) { await Task.CompletedTask; return NoRecordStore<IEnumerable<IHolon>>(); }

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsByMetaData(string metaKey, string metaValue, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0) => NoRecordStore<IEnumerable<IHolon>>();

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsByMetaDataAsync(Dictionary<string, string> metaKeyValuePairs, MetaKeyValuePairMatchMode metaKeyValuePairMatchMode, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0) { await Task.CompletedTask; return NoRecordStore<IEnumerable<IHolon>>(); }

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsByMetaData(Dictionary<string, string> metaKeyValuePairs, MetaKeyValuePairMatchMode metaKeyValuePairMatchMode, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0) => NoRecordStore<IEnumerable<IHolon>>();

    }
}
