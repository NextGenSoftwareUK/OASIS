using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace NextGenSoftware.OASIS.API.Providers.SuiOASIS;

public partial class SuiOASIS
{
    /// <summary>Fail-closed callback for original signed identity verification; performs no ledger submission.</summary>
    public async Task VerifyOriginalSignedPreparationAsync(JsonElement preparation, CancellationToken token = default)
    {
        var digest = preparation.GetProperty("transactionHash").GetString();
        var sender = preparation.GetProperty("sender").GetString();
        var result = await InvokeSdkAsync("verifyNativePreparation", new { preparation }, token);
        if (result.ValueKind != JsonValueKind.Object || !result.TryGetProperty("verified", out var verified)
            || verified.ValueKind != JsonValueKind.True
            || !result.TryGetProperty("transactionHash", out var hash) || hash.ValueKind != JsonValueKind.String || hash.GetString() != digest
            || !result.TryGetProperty("sender", out var identity) || identity.ValueKind != JsonValueKind.String || identity.GetString() != sender)
            throw new InvalidDataException("Official Sui preparation verification did not bind the original digest and signer.");
    }
}
