using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace NextGenSoftware.OASIS.API.Providers.PolkadotOASIS;

public partial class PolkadotOASIS
{
    private static string NativeAccount(string value)=>IsGenesisHash(value)?value.ToLowerInvariant():
        value!=null&&Regex.IsMatch(value,"^[1-9A-HJ-NP-Za-km-z]{46,50}$",RegexOptions.CultureInvariant)?value:
        throw new ArgumentException("Explicit AccountId32 hex or SS58 native account identity required.");

    private async Task<string[]> NormalizeNativeAccountsAsync(string[] addresses,CancellationToken token)
    {
        var snapshot=addresses.ToArray();
        var present=snapshot.Where(value=>!string.IsNullOrWhiteSpace(value)).Select(NativeAccount).ToArray();
        string[] canonical;
        if(present.All(IsGenesisHash))canonical=present.Select(value=>value.ToLowerInvariant()).ToArray();
        else {
            var decoded=await InvokePolkadotSdkAsync(new { operation="canonicalAccountIdentities",addresses=present },token).ConfigureAwait(false);
            if(decoded.ValueKind!=JsonValueKind.Array||decoded.GetArrayLength()!=present.Length)
                throw new InvalidDataException("SDK account count differs from the frozen native request.");
            canonical=decoded.EnumerateArray().Select(value=>value.GetString()).ToArray();
            if(canonical.Any(value=>!IsGenesisHash(value)))throw new InvalidDataException("SDK did not return canonical AccountId32 identities.");
        }
        var index=0;
        return snapshot.Select(value=>string.IsNullOrWhiteSpace(value)?null:canonical[index++].ToLowerInvariant()).ToArray();
    }
}
