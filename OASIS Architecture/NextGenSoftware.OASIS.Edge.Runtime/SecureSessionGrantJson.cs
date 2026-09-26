using System.Text;
using System.Text.Json;
using NextGenSoftware.OASIS.API.Core.Managers.OASISHyperDrive.Synchronization;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.Edge.Runtime
{
    internal static class SecureSessionGrantJson
    {
        internal static byte[] Serialize(HyperDriveOfflineSessionGrant grant) =>
            Encoding.UTF8.GetBytes(HyperDriveJson.Serialize(grant));

        internal static OASISResult<HyperDriveOfflineSessionGrant> Deserialize(byte[] value)
        {
            try
            {
                var grant = HyperDriveJson.Deserialize<HyperDriveOfflineSessionGrant>(Encoding.UTF8.GetString(value));
                if (grant == null)
                    return Error<HyperDriveOfflineSessionGrant>("EDGE_SECURE_SESSION_INVALID", "The protected offline session is empty.");
                return new OASISResult<HyperDriveOfflineSessionGrant>(grant) { IsLoaded = true };
            }
            catch (JsonException ex)
            {
                return Error<HyperDriveOfflineSessionGrant>("EDGE_SECURE_SESSION_INVALID",
                    $"The protected offline session is invalid: {ex.Message}");
            }
        }

        internal static OASISResult<T> Error<T>(string code, string message) => new OASISResult<T>
        { IsError = true, ErrorCount = 1, ErrorCode = code, Message = message };
    }
}
