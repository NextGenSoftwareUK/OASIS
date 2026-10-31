using System.Security.Cryptography;
using System.Text.Json;
using FluentAssertions;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.Common;
using Xunit;

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.IntegrationTests;

public partial class SQLLiteDBOASISIntegrationTests
{
    public static IEnumerable<object[]> OrdinaryCountCases(){foreach(var synchronous in new[]{false,true})foreach(var batch in new[]{false,true})foreach(var children in new[]{false,true})foreach(var onProvider in new[]{false,true})yield return new object[]{synchronous,batch,children,onProvider};}
    [Theory][MemberData(nameof(OrdinaryCountCases))]
    public async Task OrdinaryGraphAcknowledgementsReportSelectedNativeCount(bool synchronous,bool batch,bool children,bool onProvider)
    {
        var pending=Path.Combine(Path.GetDirectoryName(_databasePath)!,"scoped-visibility.pending.json");
        var first=new Holon{Id=Guid.NewGuid(),Name="owned-count-first"};var second=new Holon{Id=Guid.NewGuid(),Name="owned-count-second"};
        first.Children.Add(new Holon{Id=Guid.NewGuid(),ParentHolonId=first.Id});first.Children.Add(new Holon{Id=Guid.NewGuid(),ParentHolonId=first.Id});
        second.Children.Add(new Holon{Id=Guid.NewGuid(),ParentHolonId=second.Id});
        var roots=batch?new[]{first,second}:new[]{first};var selectedIds=roots.SelectMany(root=>children?new[]{root.Id}.Concat(root.Children.Select(row=>row.Id)):new[]{root.Id}).ToArray();
        var intentBytes=JsonSerializer.SerializeToUtf8Bytes(new{ids=selectedIds,synchronous,batch,children,onProvider,state="before-owned-ordinary-count-write"});
        await using(var file=new FileStream(pending,FileMode.CreateNew,FileAccess.Write,FileShare.None,4096,FileOptions.WriteThrough)){await file.WriteAsync(intentBytes);file.Flush(true);}
        int count;
        using(var provider=new SQLLiteDBOASIS(TestConnectionString)){
            (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
            OASISResult<bool> Envelope<T>(OASISResult<T> response)=>Core.Helpers.OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult<T,bool>(response);
            var response=batch?Envelope(synchronous?provider.SaveHolons(roots,saveChildren:children,saveChildrenOnProvider:onProvider):await provider.SaveHolonsAsync(roots,saveChildren:children,saveChildrenOnProvider:onProvider))
                :Envelope(synchronous?provider.SaveHolon(first,saveChildren:children,saveChildrenOnProvider:onProvider):await provider.SaveHolonAsync(first,saveChildren:children,saveChildrenOnProvider:onProvider));
            response.IsError.Should().BeFalse(response.Exception?.ToString());response.IsSaved.Should().BeTrue();count=response.SavedCount;
            string Fingerprint()=>string.Join("|",new[]{_databasePath,_databasePath+"-wal"}.Select(path=>File.Exists(path)?Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path))):"absent"));
            var before=Fingerprint();var native=await provider.LoadAllHolonsAsync(loadChildren:false);
            native.IsError.Should().BeFalse(native.Exception?.ToString());native.Result.Select(row=>row.Id).Should().BeEquivalentTo(selectedIds);Fingerprint().Should().Be(before);
            Console.WriteLine($"Native ordinary graph acknowledged/independently read {selectedIds.Length} selected records; response SavedCount={count}.");
        }
        SHA256.HashData(await File.ReadAllBytesAsync(pending)).Should().Equal(SHA256.HashData(intentBytes));File.Delete(pending);
        count.Should().Be(selectedIds.Length,"count committed selected records, not only roots or an invented zero");
    }
}
