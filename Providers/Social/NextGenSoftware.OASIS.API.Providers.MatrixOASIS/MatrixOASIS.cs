using System;
using System.Collections.Generic;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;
using NextGenSoftware.Utilities;

namespace NextGenSoftware.OASIS.API.Providers.MatrixOASIS
{
    /// <summary>
    /// OASIS provider for the Matrix open protocol (Client-Server API v3).
    ///
    /// Storage: every OASIS record key is one state event of type <c>org.oasis.record</c> in a dedicated room, with the
    /// key as the state key. Room state keeps exactly the latest event per (type, state key), so a save replaces the
    /// record and a read returns it; a delete replaces it with empty content, Matrix's convention for removed state
    /// (state cannot be erased from room history). Values are compressed and encrypted with the configured key because
    /// homeserver operators and every room member can read room state. Listing reads the room's full current state.
    ///
    /// Authentication: an access token, or a username and password for <c>m.login.password</c>.
    /// </summary>
    public class MatrixOASIS : KeyValueStorageProviderBase, IOASISStorageProvider, IOASISNETProvider
    {
        /// <param name="homeserver">Homeserver base URL, e.g. <c>https://matrix.example.org</c>.</param>
        /// <param name="accessToken">Access token of the storage account.</param>
        /// <param name="roomId">Room that holds the records; the account must be joined with permission to send state.</param>
        /// <param name="encryptionKey">32-byte record encryption key.</param>
        public MatrixOASIS(string homeserver, string accessToken, string roomId, byte[] encryptionKey)
            : this(new MatrixRoomStateKeyValueBackend(homeserver, MatrixCredentials.FromToken(accessToken), roomId), encryptionKey) { }

        /// <summary>Logs in with <c>m.login.password</c> on first use.</summary>
        public MatrixOASIS(string homeserver, string username, string password, string roomId, byte[] encryptionKey)
            : this(new MatrixRoomStateKeyValueBackend(homeserver, MatrixCredentials.FromPassword(username, password), roomId), encryptionKey) { }

        private MatrixOASIS(MatrixRoomStateKeyValueBackend backend, byte[] encryptionKey) : base(CreateBackend(backend, encryptionKey))
        {
            ProviderName = "MatrixOASIS";
            ProviderDescription = "Matrix provider: encrypted records as room state events (Client-Server API v3).";
            ProviderType = new EnumValue<ProviderType>(Core.Enums.ProviderType.MatrixOASIS);
            ProviderCategory = new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.StorageAndNetwork);
            ProviderCapabilities.Add(new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.Network));
        }

        private static IKeyValueBackend CreateBackend(MatrixRoomStateKeyValueBackend backend, byte[] encryptionKey)
        {
            if (encryptionKey == null || encryptionKey.Length != 32)
                throw new ArgumentException("Matrix storage requires an explicit 32-byte record encryption key.", nameof(encryptionKey));
            return new PrivateKeyValueBackend(backend, encryptionKey, compress: true);
        }

        public OASISResult<IEnumerable<IAvatar>> GetAvatarsNearMe(long geoLat, long geoLong, int radiusInMeters)
        {
            var result = new OASISResult<IEnumerable<IAvatar>>();
            OASISErrorHandling.HandleError(ref result, "MatrixOASIS: Geolocation is not supported.");
            return result;
        }

        public OASISResult<IEnumerable<IHolon>> GetHolonsNearMe(long geoLat, long geoLong, int radiusInMeters, HolonType Type)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            OASISErrorHandling.HandleError(ref result, "MatrixOASIS: Geolocation is not supported.");
            return result;
        }
    }
}
