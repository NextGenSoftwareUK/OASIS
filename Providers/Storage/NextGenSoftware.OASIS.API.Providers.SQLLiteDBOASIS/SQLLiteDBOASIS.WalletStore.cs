using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.Entities;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS
{
    public partial class SQLLiteDBOASIS
    {
        private async Task<OASISResult<bool>> StoreOwnedWalletBatchAsync(Dictionary<ProviderType, List<IProviderWallet>> wallets)
        {
            var result = new OASISResult<bool>();
            var affected = new List<string>();
            try
            {
                if (wallets.Any(entry => entry.Value == null || entry.Value.Any(wallet => wallet == null || wallet.AvatarId == Guid.Empty)))
                    throw new ArgumentException("Every batch wallet requires an explicit avatar owner.");
                var groups = wallets.SelectMany(entry => entry.Value.Select(wallet => new { Provider = entry.Key, Wallet = wallet }))
                    .GroupBy(entry => entry.Wallet.AvatarId).ToList();
                await using var transaction = await _appDataContext.Database.BeginTransactionAsync();
                foreach (var group in groups)
                {
                    var owner = await _avatarRepository.LoadAvatarAsync(group.Key);
                    if (owner.IsError) { result.IsError = true; result.Exception = owner.Exception; result.ErrorCode = owner.ErrorCode; result.Message = owner.Message; return result; }
                    if (owner.Result == null) throw new ArgumentException("Batch wallet owner does not exist.");
                    var current = await ReadLocalWalletsAsync(group.Key);
                    foreach (var provider in group.GroupBy(entry => entry.Provider))
                        current[provider.Key] = provider.Select(entry => entry.Wallet).ToList();
                    affected.Add(group.Key.ToString());
                    var saved = await StoreLocalWalletsAsync(group.Key, current);
                    if (saved.IsError) return saved;
                }
                await transaction.CommitAsync();
                result.Result = true;
                result.IsSaved = true;
            }
            catch (Exception exception)
            {
                result.IsError = true; result.Exception = exception; result.Message = "Owned wallet batch failed.";
            }
            finally
            {
                foreach (var entry in _appDataContext.ChangeTracker.Entries<AvatarWalletStore>().Where(entry => affected.Contains(entry.Entity.AvatarId)).ToList())
                    entry.State = EntityState.Detached;
            }
            return result;
        }

        private async Task<OASISResult<bool>> StoreLocalWalletsAsync(Guid avatarId,
            Dictionary<ProviderType, List<IProviderWallet>> wallets)
        {
            var result = new OASISResult<bool>();
            AvatarWalletStore row = null;
            try
            {
                if (wallets == null) throw new ArgumentNullException(nameof(wallets));
                if (wallets.Any(entry => entry.Value == null || entry.Value.Any(wallet => wallet == null)))
                    throw new ArgumentException("Wallet collections cannot contain null entries.");
                var entries = wallets.SelectMany(entry => entry.Value.Select(wallet => new { Provider = entry.Key, Wallet = wallet })).ToList();
                if (entries.Any(entry => entry.Wallet.AvatarId != avatarId || entry.Wallet.WalletId == Guid.Empty))
                    throw new ArgumentException("Wallets must have a nonempty identity and match the storage avatar owner.");
                if (entries.GroupBy(entry => entry.Wallet.WalletId).Any(group => group.Count() != 1))
                    throw new ArgumentException("Wallet identities cannot be duplicated within an avatar store.");
                if (entries.Any(entry => !Enum.IsDefined(typeof(ProviderType), entry.Provider)
                    || (entry.Wallet.ProviderType != NextGenSoftware.OASIS.API.Core.Enums.ProviderType.Default && entry.Wallet.ProviderType != entry.Provider)))
                    throw new ArgumentException("Wallet provider types must match their dictionary provider.");
                var json = JsonConvert.SerializeObject(wallets, new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.None });
                row = await _appDataContext.AvatarWalletStores.SingleOrDefaultAsync(value => value.AvatarId == avatarId.ToString());
                if (row == null)
                {
                    row = new AvatarWalletStore { AvatarId = avatarId.ToString() };
                    _appDataContext.AvatarWalletStores.Add(row);
                }
                row.WalletsJson = json;
                await _appDataContext.SaveChangesAsync();
                result.Result = true;
                result.IsSaved = true;
            }
            catch (Exception exception)
            {
                if (row != null) _appDataContext.Entry(row).State = EntityState.Detached;
                result.IsError = true;
                result.Exception = exception;
                result.Message = "Local wallet persistence failed.";
            }
            return result;
        }

        private async Task<Dictionary<ProviderType, List<IProviderWallet>>> ReadLocalWalletsAsync(Guid avatarId)
        {
            var row = await _appDataContext.AvatarWalletStores.AsNoTracking()
                .SingleOrDefaultAsync(value => value.AvatarId == avatarId.ToString());
            var result = new Dictionary<ProviderType, List<IProviderWallet>>();
            if (row == null) return result;
            var json = JObject.Parse(row.WalletsJson);
            var serializer = JsonSerializer.Create(new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.None,
                Converters = { new StoredWalletTransactionConverter() }
            });
            foreach (var entry in json.Properties())
            {
                if (!Enum.TryParse<ProviderType>(entry.Name, out var provider) || !Enum.IsDefined(typeof(ProviderType), provider))
                    throw new JsonSerializationException("Invalid stored wallet provider type.");
                var wallets = entry.Value.ToObject<List<ProviderWallet>>(serializer)
                    ?? throw new JsonSerializationException("Stored wallet collection cannot be null.");
                if (wallets.Any(wallet => wallet == null || wallet.AvatarId != avatarId || wallet.WalletId == Guid.Empty
                    || (wallet.ProviderType != NextGenSoftware.OASIS.API.Core.Enums.ProviderType.Default && wallet.ProviderType != provider)))
                    throw new JsonSerializationException("Stored wallet identity, owner or provider is invalid.");
                result.Add(provider, wallets.Cast<IProviderWallet>().ToList());
            }
            if (result.Values.SelectMany(wallets => wallets).GroupBy(wallet => wallet.WalletId).Any(group => group.Count() != 1))
                throw new JsonSerializationException("Stored wallet identity is duplicated.");
            return result;
        }

        private sealed class StoredWalletTransactionConverter : JsonConverter
        {
            public override bool CanWrite => false;
            public override bool CanConvert(Type objectType) =>
                objectType == typeof(NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Responses.IWalletTransaction);
            public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer) =>
                JObject.Load(reader).ToObject<NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Response.WalletTransaction>(serializer);
            public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer) =>
                throw new NotSupportedException("Transaction serialization uses the concrete source contract.");
        }
    }
}
