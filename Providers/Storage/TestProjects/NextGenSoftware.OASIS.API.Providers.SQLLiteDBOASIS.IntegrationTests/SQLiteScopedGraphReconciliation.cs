using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.Persistence.Context;
using Xunit;

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.IntegrationTests;

public sealed class SQLiteScopedGraphReconciliation
{
    [Fact][Trait("Category","Reconciliation")]
    public async Task InspectOriginalAcceptedPartialSaveBeforeOwnedTeardown()
    {
        var root=new DirectoryInfo(AppContext.BaseDirectory);
        while(root!=null&&!File.Exists(Path.Combine(root.FullName,"Scripts/TestHosts/Run-ReusableUsageRecoveryTests.ps1")))root=root.Parent;
        Assert.NotNull(root);var work=Path.Combine(root!.FullName,"TestResults/SQLiteCurrent");
        using var lease=new FileStream(Path.Combine(work,"environment.lock"),FileMode.OpenOrCreate,FileAccess.ReadWrite,FileShare.None);
        var db=Path.Combine(work,"integration-current.db");var intent=Path.Combine(work,"scoped-visibility.pending.json");
        static string Hash(byte[] bytes)=>Convert.ToHexString(SHA256.HashData(bytes));
        Assert.Equal("D2814E623A97BEF8C363211F3272F9848939DFB393D76351065C1685607D23CC",Hash(await File.ReadAllBytesAsync(db)));
        Assert.Equal("4CA6618D687776D937FC0F23F0AA416E0D9FD0A4681E4BF1D91AFBCED4D73817",Hash(await File.ReadAllBytesAsync(intent)));
        var failure=Path.Combine(work,"scoped-visibility-final.trx");
        Assert.Equal("523CF34B42906122F637EACD89C955DB12EDF33428083B340B3BCC30C9E4CB54",Hash(await File.ReadAllBytesAsync(failure)));
        using var pending=JsonDocument.Parse(await File.ReadAllBytesAsync(intent));
        var actor=pending.RootElement.GetProperty("actor").GetGuid();var ids=pending.RootElement.GetProperty("ids").EnumerateArray().Select(row=>row.GetGuid()).ToArray();
        Assert.Equal(5,ids.Length);Assert.False(pending.RootElement.GetProperty("synchronous").GetBoolean());Assert.Equal("search",pending.RootElement.GetProperty("path").GetString());
        object nativeProof;
        // Read-only mode, no provider activation/migration/save replay or native row mutation.
        using(var native=new DataContext($"Data Source={db};Mode=ReadOnly;Pooling=False")){
            var rows=native.Holons.AsNoTracking().Select(row=>new{row.Id,row.ParentHolonId,row.CreatedByAvatarId,row.Version,row.IsPublic,row.PersistentValueJson}).ToArray();
            Assert.Equal(5,rows.Length);Assert.Equal(ids.OrderBy(id=>id),rows.Select(row=>Guid.Parse(row.Id)).OrderBy(id=>id));
            var versions=new[]{3,7,8,9,10};var parents=new[]{Guid.Empty,ids[0],ids[0],ids[0],ids[2]};
            for(var index=0;index<ids.Length;index++){
                var row=rows.Single(row=>Guid.Parse(row.Id)==ids[index]);Assert.Equal(versions[index],row.Version);Assert.Equal(parents[index],Guid.Parse(row.ParentHolonId));
                if(index is 0 or 1 or 4)Assert.Equal(actor,Guid.Parse(row.CreatedByAvatarId));else Assert.NotEqual(actor,Guid.Parse(row.CreatedByAvatarId));
            }
            Assert.True(rows.Single(row=>row.Id==ids[3].ToString()).IsPublic);
            var avatars=native.Avatars.AsNoTracking().Select(row=>new{row.Id,row.Username}).ToArray();
            Assert.Single(avatars);Assert.Equal(actor,Guid.Parse(avatars[0].Id));Assert.Equal("owned-scope-"+actor.ToString("N"),avatars[0].Username);
            nativeProof=rows.Select(row=>new{row.Id,row.ParentHolonId,row.CreatedByAvatarId,row.Version,row.IsPublic,persistentValueSha256=Hash(System.Text.Encoding.UTF8.GetBytes(row.PersistentValueJson))}).ToArray();
        }
        Assert.False(File.Exists(db+"-wal"));Assert.False(File.Exists(db+"-shm"));
        Assert.Equal("D2814E623A97BEF8C363211F3272F9848939DFB393D76351065C1685607D23CC",Hash(await File.ReadAllBytesAsync(db)));
        Assert.Equal("4CA6618D687776D937FC0F23F0AA416E0D9FD0A4681E4BF1D91AFBCED4D73817",Hash(await File.ReadAllBytesAsync(intent)));
        var proof=Path.Combine(work,"scoped-visibility-original-reconciled.json");
        await using(var file=new FileStream(proof,FileMode.CreateNew,FileAccess.Write,FileShare.None,4096,FileOptions.WriteThrough)){
            await JsonSerializer.SerializeAsync(file,new{state="original-synthetic-native-rows-inspected",actor,nativeProof,prePartialSaveFingerprintPersisted=false,
                scope="Original identities/versions/parent edges verified, NOT proof of unchanged pre-save bytes or arbitrary corruption recovery",databaseSha256="D2814E623A97BEF8C363211F3272F9848939DFB393D76351065C1685607D23CC"});file.Flush(true);
        }
        // This exact database was create-only owned by the failed synthetic test, not a user database.
        File.Delete(db);Assert.False(File.Exists(db));File.Delete(intent);Assert.False(File.Exists(intent));
        Assert.Equal("523CF34B42906122F637EACD89C955DB12EDF33428083B340B3BCC30C9E4CB54",Hash(await File.ReadAllBytesAsync(failure)));
    }
}
