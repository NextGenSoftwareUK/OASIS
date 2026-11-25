using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Globalization;
using EOSNewYork.EOSCore.Response.API;
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Providers.EOSIOOASIS.Entities.DTOs.GetAccount;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.GeoSpatialNFT;
using NextGenSoftware.OASIS.API.Core.Interfaces.Search;
using NextGenSoftware.OASIS.API.Core.Objects.Search;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.GeoSpatialNFT.Request;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Response;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Responses;
using NextGenSoftware.OASIS.API.Core.Objects.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Objects.Wallets.Response;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.DTOs;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.Enums;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Response;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Requests;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Responses;
using System.Threading;

namespace NextGenSoftware.OASIS.API.Providers.TelosOASIS
{
    public partial class TelosOASIS
    {
        public override OASISResult<IEnumerable<IAvatarDetail>> LoadAllAvatarDetails(int version = 0)
            => _eosioOASIS.LoadAllAvatarDetails(version);

        public override Task<OASISResult<IEnumerable<IAvatarDetail>>> LoadAllAvatarDetailsAsync(int version = 0)
            => _eosioOASIS.LoadAllAvatarDetailsAsync(version);

        public override OASISResult<IAvatar> SaveAvatar(IAvatar Avatar)
            => _eosioOASIS.SaveAvatar(Avatar);

        public override Task<OASISResult<IAvatar>> SaveAvatarAsync(IAvatar Avatar)
            => _eosioOASIS.SaveAvatarAsync(Avatar);

        public override OASISResult<IAvatarDetail> SaveAvatarDetail(IAvatarDetail Avatar)
            => _eosioOASIS.SaveAvatarDetail(Avatar);

        public override Task<OASISResult<IAvatarDetail>> SaveAvatarDetailAsync(IAvatarDetail Avatar)
            => _eosioOASIS.SaveAvatarDetailAsync(Avatar);

        public override OASISResult<bool> DeleteAvatar(Guid id, bool softDelete = true)
            => _eosioOASIS.DeleteAvatar(id, softDelete);

        public override OASISResult<bool> DeleteAvatarByEmail(string avatarEmail, bool softDelete = true)
            => _eosioOASIS.DeleteAvatarByEmail(avatarEmail, softDelete);

        public override OASISResult<bool> DeleteAvatarByUsername(string avatarUsername, bool softDelete = true)
            => _eosioOASIS.DeleteAvatarByUsername(avatarUsername, softDelete);

        public override Task<OASISResult<bool>> DeleteAvatarAsync(Guid id, bool softDelete = true)
            => _eosioOASIS.DeleteAvatarAsync(id, softDelete);

        public override Task<OASISResult<bool>> DeleteAvatarByEmailAsync(string avatarEmail, bool softDelete = true)
            => _eosioOASIS.DeleteAvatarByEmailAsync(avatarEmail, softDelete);

        public override Task<OASISResult<bool>> DeleteAvatarByUsernameAsync(string avatarUsername, bool softDelete = true)
            => _eosioOASIS.DeleteAvatarByUsernameAsync(avatarUsername, softDelete);

        public override OASISResult<bool> DeleteAvatar(string providerKey, bool softDelete = true)
            => _eosioOASIS.DeleteAvatar(providerKey, softDelete);

        public override Task<OASISResult<bool>> DeleteAvatarAsync(string providerKey, bool softDelete = true)
            => _eosioOASIS.DeleteAvatarAsync(providerKey, softDelete);

        public override Task<OASISResult<ISearchResults>> SearchAsync(ISearchParams searchParams, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0)
            => _eosioOASIS.SearchAsync(searchParams, loadChildren, recursive, maxChildDepth, continueOnError, version);

    }
}
