using System;
using System.Net.Http;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;
using NextGenSoftware.Utilities;

namespace NextGenSoftware.OASIS.API.Providers.PineconeOASIS
{
    /// <summary>
    /// OASIS provider for Pinecone. OASIS uses one dense, one-dimensional index as a key-value store: every OASIS
    /// record key is a vector id, the value lives in the vector's metadata, and the shared
    /// <see cref="KeyValueStorageProviderBase"/> supplies the avatar, holon, index and lifecycle semantics.
    /// The index is API-key protected and private to its Pinecone project, so records are stored as OASIS JSON
    /// (no public-store key hiding as used for public relays/ledgers).
    /// </summary>
    public partial class PineconeOASIS : KeyValueStorageProviderBase, IOASISDBStorageProvider
    {
        public PineconeOASIS(string indexHost, string apiKey)
            : this(new HttpClient(), indexHost, apiKey)
        {
        }

        /// <summary>The caller owns this dedicated HTTP client; the provider never changes its default headers.</summary>
        public PineconeOASIS(HttpClient http, string indexHost, string apiKey)
            : base(new PineconeKeyValueBackend(http, indexHost, apiKey))
        {
            ProviderName = "PineconeOASIS";
            ProviderDescription = "Pinecone vector-index key-value storage for OASIS avatars and holons.";
            ProviderType = new EnumValue<ProviderType>(Core.Enums.ProviderType.PineconeOASIS);
            ProviderCategory = new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.StorageLocalAndNetwork);
        }

        public async Task<OASISResult<bool>> DeleteHolonSoftAsync(Guid id, bool softDelete = true)
        {
            var deleted = await DeleteHolonRecordAsync(id, softDelete);
            var result = OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult<IHolon, bool>(deleted);
            result.Result = !deleted.IsError && deleted.IsDeleted;
            return result;
        }

        public OASISResult<bool> DeleteHolonSoft(Guid id, bool softDelete = true)
            => DeleteHolonSoftAsync(id, softDelete).GetAwaiter().GetResult();
    }
}
