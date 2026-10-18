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
        public override Task<OASISResult<IAvatarDetail>> LoadAvatarDetailAsync(Guid avatarId,int version=0) => ReadCanonicalDetailByIdAsync(avatarId,version);

        public override Task<OASISResult<IEnumerable<IAvatarDetail>>> LoadAllAvatarDetailsAsync(int version=0) => ReadCanonicalDetailsAsync(version);

        public override Task<OASISResult<bool>> DeleteAvatarAsync(string providerKey, bool softDelete = true)
            => DeleteCanonicalAvatarKeyAsync(providerKey,softDelete);

        public override OASISResult<IAvatarDetail> LoadAvatarDetail(Guid avatarId, int version = 0)
        {
            return LoadAvatarDetailAsync(avatarId, version).Result;
        }

        public override async Task<OASISResult<IAvatar>> LoadAvatarByProviderKeyAsync(string providerKey, int version = 0)
        {
            var result=new OASISResult<IAvatar>();
            try
            {
                ValidateFirestoreVersion(version);
                if(!Guid.TryParse(providerKey,out var id) || id==Guid.Empty)
                    throw new ArgumentException("A nonempty GUID Firestore avatar storage key is required.",nameof(providerKey));
                return await ReadCanonicalAvatarAsync(id,version);
            }
            catch(Exception cause) {OASISErrorHandling.HandleError(ref result,cause.Message,cause);}
            return result;
        }

        public override OASISResult<bool> DeleteAvatarByUsername(string avatarUsername, bool softDelete = true)
        {
            return DeleteAvatarByUsernameAsync(avatarUsername, softDelete).Result;
        }

        public override Task<OASISResult<bool>> DeleteAvatarByEmailAsync(string avatarEmail, bool softDelete = true)
            => DeleteCanonicalAvatarSelectorAsync(avatarEmail,true,softDelete);
    }
}
