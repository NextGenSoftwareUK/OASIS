using System;
using System.IO;
using System.Data;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Text;
using System.Linq;
using Newtonsoft.Json;
using Microsoft.Extensions.Configuration;
using Ipfs.Http;
using Ipfs;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Interfaces.Search;
using NextGenSoftware.OASIS.API.Core.Objects.Search;
using NextGenSoftware.Utilities;
using Ipfs.CoreApi;
using System.Xml.Linq;

namespace NextGenSoftware.OASIS.API.Providers.IPFSOASIS
{
    public partial class IPFSOASIS : OASISStorageProviderBase, IOASISStorageProvider, IOASISNETProvider
    {

        public override OASISResult<IAvatar> LoadAvatar(Guid Id, int version = 0)
        {
            return LoadAvatarAsync(Id,version).GetAwaiter().GetResult();
        }

        //public override OASISResult<IAvatar> LoadAvatar(string username, int version = 0)
        //{
        //    return LoadAvatarAsync(username).Result;
        //}

        //public override async Task<OASISResult<IAvatar>> LoadAvatarAsync(string providerKey, int version = 0)
        //{
        //    return await LoadAvatarTemplateAsync(a => a.ProviderUniqueStorageKey.Where(b => b.Value == providerKey).Any());
        //}

        public override async Task<OASISResult<IAvatar>> LoadAvatarAsync(Guid Id, int version = 0)
        {
            return await LoadAvatarTemplateAsync(a => a.Id == Id,version,Id!=Guid.Empty);
        }


        public override OASISResult<IAvatar> LoadAvatarByProviderKey(string providerKey, int version = 0)
        {
            return LoadAvatarByProviderKeyAsync(providerKey,version).GetAwaiter().GetResult();
        }

        public override async Task<OASISResult<IAvatar>> LoadAvatarByProviderKeyAsync(string providerKey, int version = 0)
        {
            //return await LoadAvatarAsync(providerKey);
            return await LoadAvatarTemplateAsync(a => a.ProviderUniqueStorageKey.TryGetValue(Core.Enums.ProviderType.IPFSOASIS,out var key) && key == providerKey,version,!string.IsNullOrWhiteSpace(providerKey));
        }


        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(string providerKey, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            if (string.IsNullOrWhiteSpace(providerKey))
                throw new ArgumentException("A parent IPFS storage key is required.", nameof(providerKey));
            if (version != 0)
                throw new NotSupportedException("Historical IPFS parent lookup is not implemented.");
            _idLookup = await LoadLookupToJson();
            var parent = FindIndexEntry("holon", row =>
                row.ProviderUniqueStorageKey.TryGetValue(Core.Enums.ProviderType.IPFSOASIS, out var key) && key == providerKey);
            return await LoadHolonsForParentAsync(parent.Value.Id, type, loadChildren, recursive,
                maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version);
        }

        public override OASISResult<IAvatarDetail> LoadAvatarDetail(Guid id, int version = 0)
        {
            return LoadAvatarDetailAsync(id,version).GetAwaiter().GetResult();
        }

        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailAsync(Guid id, int version = 0)
        {
            return await LoadAvatarDetailTemplateAsync(a => a.Id == id,version,id!=Guid.Empty);
        }

        public override OASISResult<IEnumerable<IAvatarDetail>> LoadAllAvatarDetails(int version = 0)
        {
            return LoadAllAvatarDetailsAsync(version).GetAwaiter().GetResult();
        }

        public override async Task<OASISResult<IEnumerable<IAvatarDetail>>> LoadAllAvatarDetailsAsync(int version=0)
        {
            var read=await ReadPrivateCollectionResultAsync<AvatarDetail>("avatar-detail",version);
            var result=OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult<IEnumerable<AvatarDetail>,IEnumerable<IAvatarDetail>>(read);
            result.Result=read.Result; return result;
        }

        public override OASISResult<IAvatarDetail> SaveAvatarDetail(IAvatarDetail Avatar)
        {
            return SaveAvatarDetailAsync(Avatar).Result;
        }

        public override async Task<OASISResult<IAvatarDetail>> SaveAvatarDetailAsync(IAvatarDetail avatarDetail)
        {
            return new OASISResult<IAvatarDetail>(await SaveAvatarDetailToFile(avatarDetail));
        }

        public override OASISResult<IAvatar> LoadAvatarByUsername(string avatarUsername, int version = 0)
        {
            return LoadAvatarByUsernameAsync(avatarUsername,version).GetAwaiter().GetResult();
        }

        public override async Task<OASISResult<IAvatar>> LoadAvatarByEmailAsync(string avatarEmail, int version = 0)
        {
            return await LoadAvatarTemplateAsync(a => string.Equals(a.email,avatarEmail,StringComparison.OrdinalIgnoreCase),version,!string.IsNullOrWhiteSpace(avatarEmail));
        }

        public override async Task<OASISResult<IAvatar>> LoadAvatarByUsernameAsync(string avatarUsername, int version = 0)
        {
            return await LoadAvatarTemplateAsync(a => string.Equals(a.login,avatarUsername,StringComparison.OrdinalIgnoreCase),version,!string.IsNullOrWhiteSpace(avatarUsername));
        }

        public override OASISResult<IAvatar> LoadAvatarByEmail(string avatarEmail, int version = 0)
        {
            return LoadAvatarByEmailAsync(avatarEmail,version).GetAwaiter().GetResult();
        }

        public override OASISResult<IAvatarDetail> LoadAvatarDetailByEmail(string avatarEmail, int version = 0)
        {
            return LoadAvatarDetailByEmailAsync(avatarEmail,version).GetAwaiter().GetResult();
        }

        public override OASISResult<IAvatarDetail> LoadAvatarDetailByUsername(string avatarUsername, int version = 0)
        {
            return LoadAvatarDetailByUsernameAsync(avatarUsername,version).GetAwaiter().GetResult();
        }

        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByUsernameAsync(string avatarUsername, int version = 0)
        {
            return await LoadAvatarDetailTemplateAsync(a => string.Equals(a.login,avatarUsername,StringComparison.OrdinalIgnoreCase),version,!string.IsNullOrWhiteSpace(avatarUsername));
        }

        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByEmailAsync(string avatarEmail, int version = 0)
        {
            return await LoadAvatarDetailTemplateAsync(a => string.Equals(a.email,avatarEmail,StringComparison.OrdinalIgnoreCase),version,!string.IsNullOrWhiteSpace(avatarEmail));
        }

        public override OASISResult<bool> DeleteAvatarByEmail(string avatarEmail, bool softDelete = true)
            => DeleteAvatarByEmailAsync(avatarEmail,softDelete).GetAwaiter().GetResult();

        public override OASISResult<bool> DeleteAvatarByUsername(string avatarUsername, bool softDelete = true)
            => DeleteAvatarByUsernameAsync(avatarUsername,softDelete).GetAwaiter().GetResult();

        public override Task<OASISResult<bool>> DeleteAvatarByEmailAsync(string avatarEmail, bool softDelete = true)
            => DeleteIndexedAvatarAsync(row=>string.Equals(row.email,avatarEmail,StringComparison.OrdinalIgnoreCase),softDelete,!string.IsNullOrWhiteSpace(avatarEmail));

        public override Task<OASISResult<bool>> DeleteAvatarByUsernameAsync(string avatarUsername, bool softDelete = true)
            => DeleteIndexedAvatarAsync(row=>string.Equals(row.login,avatarUsername,StringComparison.OrdinalIgnoreCase),softDelete,!string.IsNullOrWhiteSpace(avatarUsername));

        public override OASISResult<IHolon> SaveHolon(IHolon holon, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
        {
            return SaveHolonAsync(holon, saveChildren,recursive,maxChildDepth,continueOnError,saveChildrenOnProvider).GetAwaiter().GetResult();
        }

        public override async Task<OASISResult<IHolon>> SaveHolonAsync(IHolon holon, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
        {
            var saved=await SaveSelectedGraphAsync(new[] {holon},saveChildren,recursive,maxChildDepth,0,continueOnError);
            return new OASISResult<IHolon> {Result=saved.IsError ? null:holon,IsError=saved.IsError,IsSaved=saved.IsSaved,
                ErrorCode=saved.ErrorCode,Exception=saved.Exception,Message=saved.Message};
        }

        public override OASISResult<IEnumerable<IHolon>> SaveHolons(IEnumerable<IHolon> holons, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
        {
            return SaveHolonsAsync(holons,saveChildren,recursive,maxChildDepth,curentChildDepth,continueOnError,saveChildrenOnProvider).GetAwaiter().GetResult();
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> SaveHolonsAsync(IEnumerable<IHolon> holons, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
        {
            return await SaveSelectedGraphAsync(holons,saveChildren,recursive,maxChildDepth,curentChildDepth,continueOnError);
        }

        OASISResult<IEnumerable<IAvatar>> IOASISNETProvider.GetAvatarsNearMe(long geoLat, long geoLong, int radiusInMeters)
            => ReadNearbyRecords(geoLat, geoLong, radiusInMeters, () => LoadAllAvatars());

        OASISResult<IEnumerable<IHolon>> IOASISNETProvider.GetHolonsNearMe(long geoLat, long geoLong, int radiusInMeters, HolonType Type)
            => ReadNearbyRecords(geoLat, geoLong, radiusInMeters, () => LoadAllHolons(HolonType.All), Type);

        private OASISResult<IEnumerable<T>> ReadNearbyRecords<T>(long latitude, long longitude, int radius,
            Func<OASISResult<IEnumerable<T>>> load, HolonType type = HolonType.All) where T : IHolonBase
        {
            var result = new OASISResult<IEnumerable<T>>();
            try
            {
                var query = new NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.StoredRecordLocationQuery(latitude, longitude, radius);
                if (!Enum.IsDefined(typeof(HolonType), type)) throw new ArgumentOutOfRangeException(nameof(type));
                if (!IsProviderActivated)
                {
                    var activated = ActivateProviderAsync().GetAwaiter().GetResult();
                    if (activated.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, activated.Message, activated.Exception);
                        result.ErrorCode = activated.ErrorCode;
                        return result;
                    }
                }
                var loaded = load();
                if (loaded.IsError) return loaded;
                result = NextGenSoftware.OASIS.API.Core.Helpers.OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult<IEnumerable<T>, IEnumerable<T>>(loaded);
                result.Result = query.Select(loaded.Result, row => row.IsActive && !row.IsDeleted && (type == HolonType.All || row.HolonType == type));
                result.IsLoaded = true;
            }
            catch (Exception cause)
            {
                result.Result = null;
                result.IsLoaded = false;
                OASISErrorHandling.HandleError(ref result, cause.Message, cause);
            }
            return result;
        }

        public override OASISResult<ISearchResults> Search(ISearchParams searchParams, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0)
        {
			return SearchAsync(searchParams, loadChildren, recursive, maxChildDepth, continueOnError, version).GetAwaiter().GetResult();
        }

		public override async Task<OASISResult<bool>> ImportAsync(IEnumerable<IHolon> holons)
		{
			var result = new OASISResult<bool>();
            var acknowledged=new List<string>();
			try
			{
                if(holons==null) throw new ArgumentNullException(nameof(holons));
                var planned=holons.ToList();
                if(planned.Any(row=>row==null || row.Id==Guid.Empty))
                    throw new ArgumentException("Every imported holon requires a nonempty identity.",nameof(holons));
                if(planned.GroupBy(row=>row.Id).Any(group=>group.Count()>1))
                    throw new ArgumentException("Duplicate imported holon identities are not allowed.",nameof(holons));
                foreach(var holon in planned)
                {
                    var saved=await SaveHolonToFile(holon);
                    acknowledged.Add(saved.ProviderUniqueStorageKey[Core.Enums.ProviderType.IPFSOASIS]);
                }
				result.Result = true;
                result.IsSaved=true;
				result.Message = "Holons imported into IPFS successfully.";
			}
			catch (Exception ex)
			{
                if(ex is IPFSRecordPublicationException publication)
                    acknowledged.AddRange(publication.AcknowledgedRecordCids);
                var failure=acknowledged.Count>0 ? new IPFSRecordPublicationException(acknowledged,ex):ex;
				OASISErrorHandling.HandleError(ref result, $"Error importing holons into IPFS: {ex.Message}",failure);
			}
			return result;
		}

        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByIdAsync(Guid avatarId, int version = 0)
        {
            return await ReadIpfsExportAsync(avatarId,null,version);
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByUsernameAsync(string avatarUsername, int version = 0)
        {
            return await ReadIpfsExportAsync(null,row=>string.Equals(row.login,avatarUsername,StringComparison.OrdinalIgnoreCase),version,!string.IsNullOrWhiteSpace(avatarUsername));
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByEmailAsync(string avatarEmailAddress, int version = 0)
        {
            return await ReadIpfsExportAsync(null,row=>string.Equals(row.email,avatarEmailAddress,StringComparison.OrdinalIgnoreCase),version,!string.IsNullOrWhiteSpace(avatarEmailAddress));
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllAsync(int version = 0)
        {
            return await ReadIpfsExportAsync(null,null,version);
        }

		public override OASISResult<bool> Import(IEnumerable<IHolon> holons)
		{
			return ImportAsync(holons).GetAwaiter().GetResult();
		}

        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarById(Guid avatarId, int version = 0)
        {
            return ExportAllDataForAvatarByIdAsync(avatarId,version).GetAwaiter().GetResult();
        }

        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarByUsername(string avatarUsername, int version = 0)
        {
            return ExportAllDataForAvatarByUsernameAsync(avatarUsername,version).GetAwaiter().GetResult();
        }

        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarByEmail(string avatarEmailAddress, int version = 0)
        {
            return ExportAllDataForAvatarByEmailAsync(avatarEmailAddress,version).GetAwaiter().GetResult();
        }

        public override OASISResult<IEnumerable<IHolon>> ExportAll(int version = 0)
        {
            return ExportAllAsync(version).GetAwaiter().GetResult();
        }

        //public override Task<OASISResult<IHolon>> LoadHolonByCustomKeyAsync(string customKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        //{
        //    throw new NotImplementedException();
        //}

        //public override OASISResult<IHolon> LoadHolonByCustomKey(string customKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        //{
        //    throw new NotImplementedException();
        //}

        //public override Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentByCustomKeyAsync(string customKey, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        //{
        //    throw new NotImplementedException();
        //}

        //public override OASISResult<IEnumerable<IHolon>> LoadHolonsForParentByCustomKey(string customKey, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        //{
        //    throw new NotImplementedException();
        //}

        //public override Task<OASISResult<IHolon>> LoadHolonByMetaDataAsync(string metaKey, string metaValue, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        //{
        //    throw new NotImplementedException();
        //}

        //public override OASISResult<IHolon> LoadHolonByMetaData(string metaKey, string metaValue, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        //{
        //    throw new NotImplementedException();
        //}

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsByMetaDataAsync(string metaKey, string metaValue, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            return await ReadIpfsMetadataAsync(metaKey==null ? null:new Dictionary<string,string> {{metaKey,metaValue}},MetaKeyValuePairMatchMode.All,type,loadChildren,recursive,maxChildDepth,curentChildDepth,loadChildrenFromProvider,version);
        }

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsByMetaData(string metaKey, string metaValue, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            return LoadHolonsByMetaDataAsync(metaKey,metaValue,type,loadChildren,recursive,maxChildDepth,curentChildDepth,continueOnError,loadChildrenFromProvider,version).GetAwaiter().GetResult();
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsByMetaDataAsync(Dictionary<string, string> metaKeyValuePairs, MetaKeyValuePairMatchMode metaKeyValuePairMatchMode, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            return await ReadIpfsMetadataAsync(metaKeyValuePairs,metaKeyValuePairMatchMode,type,loadChildren,recursive,maxChildDepth,curentChildDepth,loadChildrenFromProvider,version);
        }

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsByMetaData(Dictionary<string, string> metaKeyValuePairs, MetaKeyValuePairMatchMode metaKeyValuePairMatchMode, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            return LoadHolonsByMetaDataAsync(metaKeyValuePairs,metaKeyValuePairMatchMode,type,loadChildren,recursive,maxChildDepth,curentChildDepth,continueOnError,loadChildrenFromProvider,version).GetAwaiter().GetResult();
        }
    }
}
