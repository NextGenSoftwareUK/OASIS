using NextGenSoftware.OASIS.API.Core;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces.Search;
using NextGenSoftware.OASIS.API.Core.Objects.Search;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;
using Neo4j.Driver;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS
{
    public partial class Neo4jOASIS
    {
        public override OASISResult<IAvatarDetail> LoadAvatarDetailByUsername(string avatarUsername, int version = 0)
        {
            return LoadAvatarDetailByUsernameAsync(avatarUsername, version).Result;
        }



        public override OASISResult<IAvatar> SaveAvatar(IAvatar Avatar)
        {
            return SaveAvatarAsync(Avatar).Result;
        }


        public override Task<OASISResult<bool>> DeleteAvatarAsync(Guid id, bool softDelete = true)
            => DeleteAvatarRecordAsync("Id", id.ToString(), softDelete);

        public override OASISResult<bool> DeleteAvatar(Guid id, bool softDelete = true)
            => DeleteAvatarAsync(id, softDelete).GetAwaiter().GetResult();

        public override Task<OASISResult<bool>> DeleteAvatarAsync(string providerKey, bool softDelete = true)
            => DeleteAvatarRecordAsync("ProviderKey", providerKey, softDelete);

        public override OASISResult<bool> DeleteAvatar(string providerKey, bool softDelete = true)
            => DeleteAvatarAsync(providerKey, softDelete).GetAwaiter().GetResult();

        public override Task<OASISResult<bool>> DeleteAvatarByEmailAsync(string avatarEmail, bool softDelete = true)
            => DeleteAvatarRecordAsync("Email", avatarEmail, softDelete);

        public override OASISResult<bool> DeleteAvatarByEmail(string avatarEmail, bool softDelete = true)
            => DeleteAvatarByEmailAsync(avatarEmail, softDelete).GetAwaiter().GetResult();

        public override Task<OASISResult<bool>> DeleteAvatarByUsernameAsync(string avatarUsername, bool softDelete = true)
            => DeleteAvatarRecordAsync("Username", avatarUsername, softDelete);

        public override OASISResult<bool> DeleteAvatarByUsername(string avatarUsername, bool softDelete = true)
            => DeleteAvatarByUsernameAsync(avatarUsername, softDelete).GetAwaiter().GetResult();



public override Task<OASISResult<ISearchResults>> SearchAsync(ISearchParams searchParams, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0)
            => SearchStoredRecordsAsync(searchParams,loadChildren,recursive,maxChildDepth,continueOnError,version);

        public override OASISResult<ISearchResults> Search(ISearchParams searchParams, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0)
        {
            return SearchAsync(searchParams, loadChildren, recursive, maxChildDepth, continueOnError, version).Result;
        }



    }
}
