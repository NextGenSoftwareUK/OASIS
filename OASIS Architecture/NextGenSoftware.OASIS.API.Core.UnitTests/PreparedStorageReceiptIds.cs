using System;
using System.Security.Cryptography;
using System.Text;

namespace NextGenSoftware.OASIS.API.Core.UnitTests;

/// <summary>Expected physical receipt identity of a prepared, non-native storage attempt.
/// PreparedStorageExecution binds the admitted operation id into the route correlation (routing labels are reusable across
/// requests, physical receipt identities are not), so tests derive the id from the operation instead of hard-coding the label.</summary>
internal static class PreparedStorageReceiptIds
{
    public static string NonNative(string operationId, string correlation, int route = 1) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes("prepared-storage-v1\n" + operationId + "\n" + correlation)))
            .ToLowerInvariant() + ".route." + route + ".non-native";
}
