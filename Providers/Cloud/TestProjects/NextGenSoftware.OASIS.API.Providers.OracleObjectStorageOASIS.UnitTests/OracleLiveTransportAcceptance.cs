using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Oci.ObjectstorageService;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.OracleObjectStorageOASIS.UnitTests;

/// <summary>Explicit live runner entry point, also exercised against the real SDK's loopback fixture.
/// This proves native transport only, not the complete provider, IAM policy or HyperDrive routing.</summary>
public static class OracleLiveTransportAcceptance
{
    public static async Task RunAsync(ObjectStorageClient client,string namespaceName,string bucket,string prefix,string work)
    {
        ArgumentNullException.ThrowIfNull(client);
        if(prefix!="oasis-acceptance/")throw new ArgumentException("Use the fixed isolated acceptance prefix.",nameof(prefix));
        if(!Directory.Exists(work))throw new DirectoryNotFoundException("Reuse the existing acceptance evidence directory.");
        var pending=Path.Combine(work,"oci-live-transport.pending.json");
        var report=Path.Combine(work,"oci-live-transport.current.json");
        if(File.Exists(pending)||File.Exists(report))throw new InvalidOperationException("Preserve/review existing evidence before another run; never replay an uncertain object mutation.");
        await using var lease=new FileStream(Path.Combine(work,"oci-live-transport.lock"),FileMode.OpenOrCreate,FileAccess.ReadWrite,FileShare.None);
        // Recheck under the SAME fixed lease, including two simultaneous invocations.
        if(File.Exists(pending)||File.Exists(report))throw new InvalidOperationException("Acceptance evidence already exists.");
        using var timeout=new CancellationTokenSource(TimeSpan.FromSeconds(45));var token=timeout.Token;
        var backend=new OracleObjectStorageBackend(client,namespaceName,bucket,prefix);
        const string key="transport-probe";var operation=Guid.NewGuid();
        var first="owned OCI transport acceptance "+operation.ToString("N");var replacement=first+" replacement";
        string Hash(string value)=>Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
        async Task Journal(string state,string? etag=null)=>await File.WriteAllTextAsync(pending,JsonSerializer.Serialize(new{
            state,operation,namespaceName,bucket,objectName=prefix+key,firstHash=Hash(first),replacementHash=Hash(replacement),etag
        }),token);
        static void Require(bool condition,string message){if(!condition)throw new InvalidDataException(message);}
        await backend.VerifyAsync(token); // Existing bucket only; no provisioning.
        Require(await backend.GetWithEntityTagAsync(key,token)==null,"Fixed acceptance object is occupied; do not adopt/delete it.");
        await Journal("before-create");
        // No catch/finally mutation cleanup: an error preserves its ORIGINAL pending journal.
        await backend.CreateOnlyAsync(key,first,token);await Journal("create-acknowledged");
        var created=await backend.GetWithEntityTagAsync(key,token);
        Require(created?.Value==first,"Created object readback does not match this operation.");
        Require((await backend.ListKeysAsync(key,token)).Contains(key,StringComparer.Ordinal),"Created object is missing from its scoped native listing.");
        await Journal("before-replace",created!.EntityTag);
        await backend.ReplaceUnchangedAsync(key,replacement,created.EntityTag,token);await Journal("replace-acknowledged");
        var updated=await backend.GetWithEntityTagAsync(key,token);
        Require(updated?.Value==replacement&&updated.EntityTag!=created.EntityTag,"Replacement readback/value/revision did not advance.");
        await Journal("before-stale-conflict",updated!.EntityTag);
        var conflicted=false;
        try{await backend.ReplaceUnchangedAsync(key,"must not be stored",created.EntityTag,token);}
        catch(StorageLifecycleConflictException){conflicted=true;}
        Require(conflicted,"Stale native ETag did not reject replacement.");
        var unchanged=await backend.GetWithEntityTagAsync(key,token);
        Require(unchanged?.Value==replacement&&unchanged.EntityTag==updated.EntityTag,"Conflict changed the owned object.");
        await Journal("before-conditional-delete",updated.EntityTag);
        await backend.DeleteUnchangedAsync(key,updated.EntityTag,token);await Journal("delete-acknowledged");
        Require(await backend.GetWithEntityTagAsync(key,token)==null,"Acknowledged deletion did not read back as explicit absence.");
        Require(!(await backend.ListKeysAsync(key,token)).Contains(key,StringComparer.Ordinal),"Deleted object remains in its scoped native listing.");
        await File.WriteAllTextAsync(report,JsonSerializer.Serialize(new{
            state="verified",operation,namespaceName,bucket,objectName=prefix+key,
            scope="native signed SDK transport only",fullProviderVerified=false,hyperDriveVerified=false,
            nominalSdkCalls=12,listPaginationMayIncreaseRequests=true,cloudCostMeasured=false,completedUtc=DateTime.UtcNow
        }),token);
        File.Delete(pending);
    }
}
