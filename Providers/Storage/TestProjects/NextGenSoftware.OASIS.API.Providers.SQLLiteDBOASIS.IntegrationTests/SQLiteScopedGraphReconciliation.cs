using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.Persistence.Context;
using Xunit;

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.IntegrationTests;

public sealed class SQLiteScopedGraphReconciliation
{
    [Fact][Trait("Category","Reconciliation")]
    public async Task InspectOriginalSavedCountAdmissionBeforeOwnedTeardown()
    {
        var root=new DirectoryInfo(AppContext.BaseDirectory);
        while(root!=null&&!File.Exists(Path.Combine(root.FullName,"Scripts/TestHosts/Run-ReusableUsageRecoveryTests.ps1")))root=root.Parent;
        Assert.NotNull(root);var work=Path.Combine(root!.FullName,"TestResults/SQLiteCurrent");
        using var lease=new FileStream(Path.Combine(work,"environment.lock"),FileMode.OpenOrCreate,FileAccess.ReadWrite,FileShare.None);
        var db=Path.Combine(work,"integration-current.db");var intent=Path.Combine(work,"scoped-visibility.pending.json");var report=Path.Combine(work,"query-snapshot-red.trx");
        static string Hash(byte[] bytes)=>Convert.ToHexString(SHA256.HashData(bytes));
        Assert.Equal("BB350744F552BEC3AAE304111CFAEF30C0CCABE733795252A160CE067F703E47",Hash(await File.ReadAllBytesAsync(db)));
        Assert.Equal("E9572E7791BE27A0AD53D682DC4F886D17EB0188040C7F80F8C52E51BE11EDA5",Hash(await File.ReadAllBytesAsync(intent)));
        Assert.Equal("DA36A6F9733463F68A621EC04AED4867D2785E89C8E01A24FC1D171BE28C451C",Hash(await File.ReadAllBytesAsync(report)));
        var xml=System.Xml.Linq.XDocument.Load(report);System.Xml.Linq.XNamespace ns="http://microsoft.com/schemas/VisualStudio/TeamTest/2010";
        var errors=xml.Descendants(ns+"Message").Select(value=>value.Value).ToArray();
        Assert.Single(errors.Where(value=>value.Contains("Expected saved.SavedCount to be 1, but found 0")));
        Assert.Equal(23,errors.Count(value=>value.Contains("Existing fixture DB/intent requires inspection")));
        using var pending=JsonDocument.Parse(await File.ReadAllBytesAsync(intent));
        var owner=pending.RootElement.GetProperty("owner").GetGuid();var ids=pending.RootElement.GetProperty("ids").EnumerateArray().Select(value=>value.GetGuid()).ToArray();
        Assert.Equal(5,ids.Length);Assert.Equal("before-owned-query-snapshot-native-writes",pending.RootElement.GetProperty("state").GetString());
        object observation;
        using(var native=new DataContext($"Data Source={db};Mode=ReadOnly;Pooling=False")){
            var rows=native.Holons.AsNoTracking().Select(row=>new{row.Id,row.Name,row.Description,row.Version,row.CreatedByAvatarId,row.ParentHolonId,row.PersistentValueJson}).ToArray();
            Assert.Single(rows);var row=rows[0];Assert.Equal(ids[0],Guid.Parse(row.Id));Assert.Equal(owner,Guid.Parse(row.CreatedByAvatarId));
            Assert.Equal("oak",row.Name);Assert.Equal("other",row.Description);Assert.Equal(3,row.Version);Assert.NotEqual(Guid.Empty,Guid.Parse(row.ParentHolonId));
            Assert.Equal(0,native.Avatars.Count());Assert.Equal(0,native.AvatarDetails.Count());
            observation=new{row.Id,row.Name,row.Version,row.CreatedByAvatarId,row.ParentHolonId,persistentValueSha256=Hash(System.Text.Encoding.UTF8.GetBytes(row.PersistentValueJson))};
        }
        Assert.False(File.Exists(db+"-wal"));Assert.False(File.Exists(db+"-shm"));
        Assert.Equal("BB350744F552BEC3AAE304111CFAEF30C0CCABE733795252A160CE067F703E47",Hash(await File.ReadAllBytesAsync(db)));
        Assert.Equal("E9572E7791BE27A0AD53D682DC4F886D17EB0188040C7F80F8C52E51BE11EDA5",Hash(await File.ReadAllBytesAsync(intent)));
        await using(var file=new FileStream(Path.Combine(work,"query-snapshot-savedcount-original-reconciled.json"),FileMode.CreateNew,FileAccess.Write,FileShare.None,4096,FileOptions.WriteThrough)){
            await JsonSerializer.SerializeAsync(file,new{state="original-successful-native-row-reconciled",reportedSavedCount=0,observedNativeCount=1,observation,noReplay=true,readOnly=true});file.Flush(true);
        }
        File.Delete(db);Assert.False(File.Exists(db));File.Delete(intent);Assert.False(File.Exists(intent));
        Assert.Equal("DA36A6F9733463F68A621EC04AED4867D2785E89C8E01A24FC1D171BE28C451C",Hash(await File.ReadAllBytesAsync(report)));
    }

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
