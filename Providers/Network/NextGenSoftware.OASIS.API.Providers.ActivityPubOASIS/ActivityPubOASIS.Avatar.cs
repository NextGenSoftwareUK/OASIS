using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Threading;
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces.Search;
using NextGenSoftware.OASIS.API.Core.Objects.Search;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.Utilities;

namespace NextGenSoftware.OASIS.API.Providers.ActivityPubOASIS
{
    public partial class ActivityPubOASIS
    {

        public override Task<OASISResult<bool>> ActivateProviderAsync()
            => ActivateQuerySourceAsync(CancellationToken.None);

        public async Task<OASISResult<bool>> ActivateQuerySourceAsync(CancellationToken cancellationToken = default)
        {
            var response = new OASISResult<bool>();

            try
            {
                ObjectDisposedException.ThrowIf(_disposed, this);
                cancellationToken.ThrowIfCancellationRequested();
                if (_isActivated)
                {
                    response.Result = true;
                    response.Message = "ActivityPub provider is already activated";
                    return response;
                }

                // This adapter uses Mastodon REST, not universal ActivityPub C2S.
                using var testResponse = await _httpClient.GetAsync("/api/v2/instance", HttpCompletionOption.ResponseHeadersRead, cancellationToken);
                testResponse.EnsureSuccessStatusCode();
                using var document=await ReadCqlStatusDocumentAsync(testResponse,cancellationToken);
                var server=document.RootElement;
                if(server.ValueKind!=JsonValueKind.Object)
                    throw new System.IO.InvalidDataException("Expected a Mastodon v2 instance object.");
                foreach(var field in new[]{"domain","title","version"})
                    if(!server.TryGetProperty(field,out var value)||value.ValueKind!=JsonValueKind.String||string.IsNullOrWhiteSpace(value.GetString()))
                        throw new System.IO.InvalidDataException("Missing Mastodon instance field: "+field);
                if(!Version.TryParse(server.GetProperty("version").GetString().Split('-')[0],out var serverVersion)||serverVersion.Major<4)
                    throw new NotSupportedException("The Mastodon REST adapter requires version 4 or newer and the v2 instance contract.");
                if(!server.TryGetProperty("configuration",out var configuration)||configuration.ValueKind!=JsonValueKind.Object)
                    throw new System.IO.InvalidDataException("Missing Mastodon instance configuration object.");
                    cancellationToken.ThrowIfCancellationRequested();
                    _isActivated = true;
                    IsProviderActivated = true;
                    response.Result = true;
                    response.Message = "Mastodon v2 instance contract verified; authenticated operations require their own authorization.";
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error activating ActivityPub provider: {ex.Message}");
            }

            return response;
        }

        public override OASISResult<bool> ActivateProvider()
        {
            return ActivateProviderAsync().Result;
        }

        public override async Task<OASISResult<bool>> DeActivateProviderAsync()
        {
            var response = new OASISResult<bool>();

            try
            {
                _isActivated = false;
                IsProviderActivated = false;
                response.Result = true;
                response.Message = "ActivityPub provider deactivated successfully";
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error deactivating ActivityPub provider: {ex.Message}");
            }

            return response;
        }

        public override OASISResult<bool> DeActivateProvider()
        {
            return DeActivateProviderAsync().Result;
        }

        public override Task<OASISResult<IAvatar>> LoadAvatarAsync(Guid id, int version = 0)
            => Task.FromResult(RejectProjectionGuid<IAvatar>());

        public override OASISResult<IAvatar> LoadAvatar(Guid id, int version = 0)
        {
            return LoadAvatarAsync(id, version).Result;
        }

        public override async Task<OASISResult<IAvatar>> LoadAvatarByProviderKeyAsync(string providerKey, int version = 0)
            => await ReadMastodonAccountAsync(providerKey,true,version);

        public override OASISResult<IAvatar> LoadAvatarByProviderKey(string providerKey, int version = 0)
        {
            return LoadAvatarByProviderKeyAsync(providerKey, version).Result;
        }

        public override Task<OASISResult<IAvatar>> LoadAvatarByEmailAsync(string avatarEmail, int version = 0)
            => Task.FromResult(RejectPrivateEmailLookup<IAvatar>());

        public override OASISResult<IAvatar> LoadAvatarByEmail(string avatarEmail, int version = 0)
        {
            return LoadAvatarByEmailAsync(avatarEmail, version).Result;
        }

        public override async Task<OASISResult<IAvatar>> LoadAvatarByUsernameAsync(string avatarUsername, int version = 0)
            => await ReadMastodonAccountAsync(avatarUsername,false,version);

        public override OASISResult<IAvatar> LoadAvatarByUsername(string avatarUsername, int version = 0)
        {
            return LoadAvatarByUsernameAsync(avatarUsername, version).Result;
        }

        public override Task<OASISResult<IAvatarDetail>> LoadAvatarDetailAsync(Guid id, int version = 0)
            => Task.FromResult(RejectProjectionGuid<IAvatarDetail>());

        public override OASISResult<IAvatarDetail> LoadAvatarDetail(Guid id, int version = 0)
        {
            return LoadAvatarDetailAsync(id, version).Result;
        }

        public override Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByEmailAsync(string avatarEmail, int version = 0)
            => Task.FromResult(RejectPrivateEmailLookup<IAvatarDetail>());

        private static OASISResult<T> RejectPrivateEmailLookup<T>()
        {
            const string message="Mastodon public accounts do not provide private email identity lookup. An email must not be sent as a public account-search query. Use a private OASIS storage provider for email lookup; no request was sent.";
            var result=new OASISResult<T>();
            OASISErrorHandling.HandleError(ref result,message,new NotSupportedException(message));
            return result;
        }

        public override OASISResult<IAvatarDetail> LoadAvatarDetailByEmail(string avatarEmail, int version = 0)
        {
            return LoadAvatarDetailByEmailAsync(avatarEmail, version).Result;
        }

        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByUsernameAsync(string avatarUsername, int version = 0)
            => await ReadMastodonDetailAsync(avatarUsername,version);

        public override OASISResult<IAvatarDetail> LoadAvatarDetailByUsername(string avatarUsername, int version = 0)
        {
            return LoadAvatarDetailByUsernameAsync(avatarUsername, version).Result;
        }

        private static OASISResult<T> RejectNativeAccountCollection<T>()
        {
            const string message="Mastodon provides explicit account-ID batches, not a complete account collection or geographic record index. Use LoadAccountBatch/LoadAccountDetailBatch with native IDs; no request was sent.";
            var result=new OASISResult<T>();
            OASISErrorHandling.HandleError(ref result,message,new NotSupportedException(message));return result;
        }
        public override Task<OASISResult<IEnumerable<IAvatar>>> LoadAllAvatarsAsync(int version=0)
            =>Task.FromResult(RejectNativeAccountCollection<IEnumerable<IAvatar>>());
        public override OASISResult<IEnumerable<IAvatar>> LoadAllAvatars(int version=0)
            =>RejectNativeAccountCollection<IEnumerable<IAvatar>>();
        public override Task<OASISResult<IEnumerable<IAvatarDetail>>> LoadAllAvatarDetailsAsync(int version=0)
            =>Task.FromResult(RejectNativeAccountCollection<IEnumerable<IAvatarDetail>>());
        public override OASISResult<IEnumerable<IAvatarDetail>> LoadAllAvatarDetails(int version=0)
            =>RejectNativeAccountCollection<IEnumerable<IAvatarDetail>>();

        public override Task<OASISResult<IAvatar>> SaveAvatarAsync(IAvatar avatar)
            => Task.FromResult(RejectPrivateRecordStorage<IAvatar>());

        private static OASISResult<T> RejectPrivateRecordStorage<T>()
        {
            const string message = "ActivityPub does not provide private OASIS record storage. Account registration and public status publication are separate consented operations, not generic record saves/imports. Use a storage provider for private records; no data was sent.";
            var result = new OASISResult<T>();
            OASISErrorHandling.HandleError(ref result, message, new NotSupportedException(message));
            return result;
        }

        public override OASISResult<IAvatar> SaveAvatar(IAvatar avatar)
        {
            return SaveAvatarAsync(avatar).Result;
        }

        public override Task<OASISResult<IAvatarDetail>> SaveAvatarDetailAsync(IAvatarDetail avatarDetail)
            => Task.FromResult(RejectPrivateRecordStorage<IAvatarDetail>());

        public override OASISResult<IAvatarDetail> SaveAvatarDetail(IAvatarDetail avatarDetail)
        {
            return SaveAvatarDetailAsync(avatarDetail).Result;
        }

        private static OASISResult<bool> RejectGenericAccountDeletion()
        {
            const string message="Generic OASIS avatar deletion is not remote account deletion or profile-image deletion. This adapter has no private avatar record to soft-delete; no remote request was sent.";
            var result=new OASISResult<bool>();
            OASISErrorHandling.HandleError(ref result,message,new NotSupportedException(message));return result;
        }

        public override Task<OASISResult<bool>> DeleteAvatarAsync(Guid id, bool softDelete = true)
            => Task.FromResult(RejectGenericAccountDeletion());

        public override OASISResult<bool> DeleteAvatar(Guid id, bool softDelete = true)
        {
            return DeleteAvatarAsync(id, softDelete).Result;
        }

        public override Task<OASISResult<bool>> DeleteAvatarAsync(string providerKey, bool softDelete = true)
            => Task.FromResult(RejectGenericAccountDeletion());

        public override OASISResult<bool> DeleteAvatar(string providerKey, bool softDelete = true)
        {
            return DeleteAvatarAsync(providerKey, softDelete).Result;
        }

        public override Task<OASISResult<bool>> DeleteAvatarByEmailAsync(string avatarEmail, bool softDelete = true)
            => Task.FromResult(RejectGenericAccountDeletion());

        public override OASISResult<bool> DeleteAvatarByEmail(string avatarEmail, bool softDelete = true)
        {
            return DeleteAvatarByEmailAsync(avatarEmail, softDelete).Result;
        }

        public override Task<OASISResult<bool>> DeleteAvatarByUsernameAsync(string avatarUsername, bool softDelete = true)
            => Task.FromResult(RejectGenericAccountDeletion());

        public override OASISResult<bool> DeleteAvatarByUsername(string avatarUsername, bool softDelete = true)
        {
            return DeleteAvatarByUsernameAsync(avatarUsername, softDelete).Result;
        }

    }
}
