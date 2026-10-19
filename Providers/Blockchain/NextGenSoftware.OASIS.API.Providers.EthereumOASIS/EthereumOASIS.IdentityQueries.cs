using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;

namespace NextGenSoftware.OASIS.API.Providers.EthereumOASIS
{
    public partial class EthereumOASIS
    {
        private static async Task<OASISResult<T>> LoadProviderIdentityAsync<T>(string providerKey,int version,
            Func<Guid,Task<OASISResult<T>>> load) where T : class,NextGenSoftware.OASIS.API.Core.Interfaces.IHolonBase
        {
            var error = new OASISResult<T>();
            try
            {
                if (!Guid.TryParseExact(providerKey,"D",out var id) || id == Guid.Empty)
                    throw new ArgumentException("Ethereum provider key must be a nonempty D-format record GUID.",nameof(providerKey));
                if(version<0)throw new ArgumentOutOfRangeException(nameof(version));
                var result = await load(id);
                if (!result.IsError && result.Result != null)
                {
                    result.Result.ProviderUniqueStorageKey ??= new Dictionary<NextGenSoftware.OASIS.API.Core.Enums.ProviderType,string>();
                    result.Result.ProviderUniqueStorageKey[NextGenSoftware.OASIS.API.Core.Enums.ProviderType.EthereumOASIS] = id.ToString();
                }
                return result;
            }
            catch (Exception cause)
            {
                OASISErrorHandling.HandleError(ref error,$"Ethereum provider-key lookup failed: {cause.Message}",cause);
                return error;
            }
        }

        private static async Task<OASISResult<T>> FindStoredIdentityAsync<T>(string value,Func<T,string> select,
            Func<Task<OASISResult<IEnumerable<T>>>> load,StringComparison comparison = StringComparison.OrdinalIgnoreCase) where T : class
        {
            var result = new OASISResult<T>();
            try
            {
                if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException("A nonempty identity query is required.",nameof(value));
                var stored = await load();
                if (stored.IsError)
                {
                    OASISErrorHandling.HandleError(ref result,stored.Message,stored.Exception);
                    result.ErrorCode = stored.ErrorCode;
                    return result;
                }
                if (stored.Result == null) throw new InvalidOperationException("Native identity collection returned no result.");
                // Equality matches account identity semantics, not substring/search matching.
                var matches = stored.Result.Where(row=>string.Equals(select(row),value,comparison)).Take(2).ToList();
                if (matches.Count > 1) throw new StorageIdentityAmbiguousException();
                if (matches.Count == 0) throw new KeyNotFoundException("No stored Ethereum identity matches the requested field.");
                result.Result = matches[0];
                result.IsLoaded = true;
            }
            catch (Exception error)
            {
                OASISErrorHandling.HandleError(ref result,$"Ethereum stored identity lookup failed: {error.Message}",error);
                if (error is StorageIdentityAmbiguousException) result.ErrorCode = OASISResultErrorCodes.AmbiguousIdentity;
                if (error is KeyNotFoundException) result.ErrorCode = OASISResultErrorCodes.RecordNotFound;
            }
            return result;
        }
    }
}
