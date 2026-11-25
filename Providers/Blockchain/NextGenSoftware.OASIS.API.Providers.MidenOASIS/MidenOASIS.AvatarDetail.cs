using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.Avatar;
using NextGenSoftware.OASIS.API.Core.Interfaces.Search;
using NextGenSoftware.OASIS.API.Core.Objects.Search;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Requests;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Responses;
using LockWeb3TokenRequest = NextGenSoftware.OASIS.API.Core.Objects.Wallet.Requests.LockWeb3TokenRequest;
using NextGenSoftware.OASIS.API.Core.Objects.Wallets;
using NextGenSoftware.OASIS.API.Core.Objects.Wallets.Response;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet;
using NextGenSoftware.Utilities;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.DTOs;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.Enums;
using NextGenSoftware.OASIS.API.Core.Utilities;
using NextGenSoftware.OASIS.API.Providers.MidenOASIS.Infrastructure.Services.Miden;
using NextGenSoftware.OASIS.API.Providers.MidenOASIS.Models;
using NextGenSoftware.OASIS.Common;
using System.Text.Json;

namespace NextGenSoftware.OASIS.API.Providers.MidenOASIS
{
    public partial class MidenOASIS
    {
        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByEmailAsync(string email, int version = 0) { await Task.CompletedTask; return NoRecordStore<IAvatarDetail>(); }

        public override OASISResult<IAvatarDetail> LoadAvatarDetailByEmail(string email, int version = 0) => NoRecordStore<IAvatarDetail>();

        public override async Task<OASISResult<bool>> DeleteAvatarAsync(string providerKey, bool softDelete = true) { await Task.CompletedTask; return NoRecordStore<bool>(); }

        public override OASISResult<bool> DeleteAvatar(string providerKey, bool softDelete = true) => NoRecordStore<bool>();

        public override OASISResult<IHolon> LoadHolon(string providerKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0) => NoRecordStore<IHolon>();

        public override async Task<OASISResult<IHolon>> LoadHolonAsync(string providerKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0) { await Task.CompletedTask; return NoRecordStore<IHolon>(); }

        public override async Task<OASISResult<IAvatar>> LoadAvatarByProviderKeyAsync(string providerKey, int version = 0) { await Task.CompletedTask; return NoRecordStore<IAvatar>(); }

        public override OASISResult<IAvatar> LoadAvatarByProviderKey(string providerKey, int version = 0) => NoRecordStore<IAvatar>();

        public override async Task<OASISResult<IAvatar>> LoadAvatarByEmailAsync(string email, int version = 0) { await Task.CompletedTask; return NoRecordStore<IAvatar>(); }

        public override OASISResult<IAvatar> LoadAvatarByEmail(string email, int version = 0) => NoRecordStore<IAvatar>();

        public override OASISResult<IAvatarDetail> SaveAvatarDetail(IAvatarDetail avatarDetail) => NoRecordStore<IAvatarDetail>();

        public override async Task<OASISResult<IAvatarDetail>> SaveAvatarDetailAsync(IAvatarDetail avatarDetail) { await Task.CompletedTask; return NoRecordStore<IAvatarDetail>(); }

        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByIdAsync(Guid avatarId, int version = 0) { await Task.CompletedTask; return NoRecordStore<IEnumerable<IHolon>>(); }

        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarById(Guid avatarId, int version = 0) => NoRecordStore<IEnumerable<IHolon>>();

        public override OASISResult<IEnumerable<IHolon>> LoadAllHolons(HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0) => NoRecordStore<IEnumerable<IHolon>>();

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadAllHolonsAsync(HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0) { await Task.CompletedTask; return NoRecordStore<IEnumerable<IHolon>>(); }

        public override OASISResult<IHolon> LoadHolon(Guid id, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0) => NoRecordStore<IHolon>();

        public override async Task<OASISResult<IHolon>> LoadHolonAsync(Guid id, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0) { await Task.CompletedTask; return NoRecordStore<IHolon>(); }

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsForParent(string providerKey, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0) => NoRecordStore<IEnumerable<IHolon>>();

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(string providerKey, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0) { await Task.CompletedTask; return NoRecordStore<IEnumerable<IHolon>>(); }

        public override async Task<OASISResult<bool>> DeleteAvatarByEmailAsync(string avatarEmail, bool softDelete = true) { await Task.CompletedTask; return NoRecordStore<bool>(); }

        public override OASISResult<bool> DeleteAvatarByEmail(string avatarEmail, bool softDelete = true) => NoRecordStore<bool>();

        public override async Task<OASISResult<IAvatar>> SaveAvatarAsync(IAvatar Avatar) { await Task.CompletedTask; return NoRecordStore<IAvatar>(); }

        public override OASISResult<IAvatar> SaveAvatar(IAvatar Avatar) => NoRecordStore<IAvatar>();
    }
}
