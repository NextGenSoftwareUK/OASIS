using System;
using System.IO;
using System.Data;
using System.Text;
using System.Linq;
using Newtonsoft.Json;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Google.Cloud.Storage.V1;
using Google.Cloud.Firestore;
using Google.Cloud.BigQuery.V2;
using NextGenSoftware.Utilities;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Interfaces.Search;
using NextGenSoftware.OASIS.API.Core.Objects.Search;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.Avatar;

namespace NextGenSoftware.OASIS.API.Providers.GoogleCloudOASIS
{
    public partial class GoogleCloudOASIS
    {
        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarByUsername(string avatarUsername, int version = 0)
        {
            return ExportAllDataForAvatarByUsernameAsync(avatarUsername, version).Result;
        }

        public override Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByEmailAsync(string avatarEmail, int version = 0)
            => ExportCanonicalSelectedAvatarAsync(avatarEmail,true,version);

        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarByEmail(string avatarEmail, int version = 0)
        {
            return ExportAllDataForAvatarByEmailAsync(avatarEmail, version).Result;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllAsync(int version = 0)
        {
            if(version!=-1)return await LoadAllHolonsAsync(loadChildrenFromProvider:true,version:version);
            var result=new OASISResult<IEnumerable<IHolon>>();
            try {
                var ready=await EnsureDetailStoreAsync(0);
                if(ready.IsError)return OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult<bool,IEnumerable<IHolon>>(ready);
                var snapshot=await _firestoreDb.Collection("holons").GetSnapshotAsync();
                var rows=(await ReadFirestoreCollectionAsync<Holon>(snapshot,version,exportAllVersions:true)).Where(row=>!row.IsDeleted).Cast<IHolon>().ToList();
                result.Result=rows;result.IsLoaded=true;result.LoadedCount=rows.Count;
            }catch(Exception cause){OASISErrorHandling.HandleError(ref result,cause.Message,cause);}
            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> ExportAll(int version = 0)
        {
            return ExportAllAsync(version).Result;
        }

        // Missing abstract method implementations
        public override Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByEmailAsync(string avatarEmail, int version = 0)
            => ReadCanonicalDetailSelectorAsync("email",avatarEmail,version);

        public override OASISResult<IAvatarDetail> LoadAvatarDetailByEmail(string avatarEmail, int version = 0)
        {
            return LoadAvatarDetailByEmailAsync(avatarEmail, version).Result;
        }

        public override OASISResult<IAvatar> LoadAvatarByProviderKey(string providerKey, int version = 0)
        {
            return LoadAvatarByProviderKeyAsync(providerKey, version).Result;
        }

        public override OASISResult<bool> DeleteAvatarByEmail(string avatarEmail, bool softDelete = true)
        {
            return DeleteAvatarByEmailAsync(avatarEmail, softDelete).Result;
        }

        public override OASISResult<bool> DeleteAvatar(string providerKey, bool softDelete = true)
        {
            return DeleteAvatarAsync(providerKey, softDelete).Result;
        }

        public override Task<OASISResult<bool>> DeleteAvatarByUsernameAsync(string avatarUsername, bool softDelete = true)
            => DeleteCanonicalAvatarSelectorAsync(avatarUsername,false,softDelete);

        public override OASISResult<IEnumerable<IAvatarDetail>> LoadAllAvatarDetails(int version = 0)
        {
            return LoadAllAvatarDetailsAsync(version).Result;
        }

        public override OASISResult<IAvatarDetail> LoadAvatarDetailByUsername(string avatarUsername, int version = 0)
        {
            return LoadAvatarDetailByUsernameAsync(avatarUsername, version).Result;
        }

        public override Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByUsernameAsync(string avatarUsername, int version = 0)
            => ReadCanonicalDetailSelectorAsync("username",avatarUsername,version);

    }
}
