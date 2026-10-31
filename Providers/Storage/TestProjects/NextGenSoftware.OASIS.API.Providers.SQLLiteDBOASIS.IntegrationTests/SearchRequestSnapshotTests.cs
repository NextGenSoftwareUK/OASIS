using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Security.Cryptography;
using System.Text.Json;
using FluentAssertions;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Objects.Search;
using NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.Interfaces;
using NextGenSoftware.OASIS.Common;
using Xunit;

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.IntegrationTests;

public partial class SQLLiteDBOASISIntegrationTests
{
    public class QueryReadBoundaryProxy:DispatchProxy
    {
        public IHolonRepository Inner=null!;public Action? OnRead;public int ReadCalls;public Guid[]? FirstNativeIds;
        protected override object? Invoke(MethodInfo? method,object?[]? args)
        {
            object? result;
            try{result=method!.Invoke(Inner,args);}
            catch(TargetInvocationException cause)when(cause.InnerException!=null){ExceptionDispatchInfo.Capture(cause.InnerException).Throw();throw;}
            return method!.Name==nameof(IHolonRepository.LoadAllHolonsAsync)
                ? Observe((Task<OASISResult<IEnumerable<IHolon>>>)result!):result;
        }
        private async Task<OASISResult<IEnumerable<IHolon>>> Observe(Task<OASISResult<IEnumerable<IHolon>>> native)
        {
            var result=await native;ReadCalls++;FirstNativeIds??=result.Result?.Select(row=>row.Id).ToArray();var callback=OnRead;OnRead=null;callback?.Invoke();return result;
        }
    }

    public static IEnumerable<object[]> QuerySnapshotCases(){foreach(var synchronous in new[]{false,true})foreach(var version in new[]{0,3})foreach(var scenario in Enumerable.Range(0,6))yield return new object[]{synchronous,version,scenario};}
    [Theory][MemberData(nameof(QuerySnapshotCases))]
    public async Task SearchKeepsOriginalRequestAcrossNativeReadBoundary(bool synchronous,int version,int scenario)
    {
        var pending=Path.Combine(Path.GetDirectoryName(_databasePath)!,"scoped-visibility.pending.json");
        var baselinePath=pending+".read-baseline.json";
        var owner=Guid.NewGuid();var foreign=Guid.NewGuid();var parent=Guid.NewGuid();var alternate=Guid.NewGuid();
        Holon Row(string name,string description,Guid actor,Guid link,string zone)=>new(){Id=Guid.NewGuid(),Name=name,Description=description,CreatedByAvatarId=actor,ParentHolonId=link,Version=3,MetaData=new(){{"zone",zone}}};
        var target=Row("oak","other",owner,parent,"north");
        var rows=new[]{target,Row("oak","other",foreign,parent,"north"),Row("pine","oak",owner,alternate,"south"),Row("other","oak",owner,parent,"north"),Row("oak","other",owner,parent,"south")};
        var intentBytes=JsonSerializer.SerializeToUtf8Bytes(new{owner,ids=rows.Select(row=>row.Id),synchronous,version,scenario,state="before-owned-query-snapshot-native-writes"});
        await using(var file=new FileStream(pending,FileMode.CreateNew,FileAccess.Write,FileShare.None,4096,FileOptions.WriteThrough)){await file.WriteAsync(intentBytes);file.Flush(true);}
        OASISResult<Core.Interfaces.Search.ISearchResults> selected;byte[] baselineBytes;
        using(var provider=new SQLLiteDBOASIS(TestConnectionString)){
            (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
            foreach(var row in rows){var saved=await provider.SaveHolonAsync(row,saveChildren:false);saved.IsError.Should().BeFalse(saved.Exception?.ToString());saved.IsSaved.Should().BeTrue();saved.SavedCount.Should().Be(1);}
            string Fingerprint()=>string.Join("|",new[]{_databasePath,_databasePath+"-wal"}.Select(file=>File.Exists(file)?Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(file))):"absent"));
            var before=Fingerprint();baselineBytes=JsonSerializer.SerializeToUtf8Bytes(new{fingerprint=before,state="acknowledged-native-rows-before-search"});
            await using(var file=new FileStream(baselinePath,FileMode.CreateNew,FileAccess.Write,FileShare.None,4096,FileOptions.WriteThrough)){await file.WriteAsync(baselineBytes);file.Flush(true);}
            var group=new SearchTextGroup{SearchQuery="oak",SearchHolons=true,HolonSearchParams=new SearchHolonParams{Name=true}};
            var query=new SearchParams{AvatarId=owner,SearchOnlyForCurrentAvatar=true,ParentId=parent,Recursive=false,FilterByMetaData=new(){{"zone","north"}},SearchGroups=new(){group}};
            var field=typeof(SQLLiteDBOASIS).GetField("_holonRepository",BindingFlags.Instance|BindingFlags.NonPublic)!;
            var original=(IHolonRepository)field.GetValue(provider)!;
            var proxy=DispatchProxy.Create<IHolonRepository,QueryReadBoundaryProxy>();var observed=(QueryReadBoundaryProxy)(object)proxy;observed.Inner=original;
            observed.OnRead=()=>{switch(scenario){case 0:query.AvatarId=foreign;break;case 1:group.SearchQuery="pine";query.ParentId=alternate;query.FilterByMetaData["zone"]="south";break;case 2:group.HolonSearchParams.Name=false;group.HolonSearchParams.Description=true;break;case 3:query.FilterByMetaData["zone"]="south";break;case 4:query.ParentId=alternate;break;case 5:query.SearchOnlyForCurrentAvatar=false;break;}};
            field.SetValue(provider,proxy);
            try{selected=synchronous?provider.Search(query,loadChildren:false,version:version):await provider.SearchAsync(query,loadChildren:false,version:version);}
            finally{field.SetValue(provider,original);}
            observed.ReadCalls.Should().BeGreaterThan(0);observed.OnRead.Should().BeNull();observed.FirstNativeIds.Should().BeEquivalentTo(rows.Select(row=>row.Id));
            selected.IsError.Should().BeFalse(selected.Exception?.ToString());Fingerprint().Should().Be(before);
        }
        SHA256.HashData(await File.ReadAllBytesAsync(baselinePath)).Should().Equal(SHA256.HashData(baselineBytes));
        SHA256.HashData(await File.ReadAllBytesAsync(pending)).Should().Equal(SHA256.HashData(intentBytes));
        File.Delete(baselinePath);File.Delete(pending);
        // Only after unchanged native bytes and safe owned teardown admission, assert output semantics.
        selected.IsLoaded.Should().BeTrue();selected.LoadedCount.Should().Be(1);selected.Result.SearchResultHolons.Select(row=>row.Id).Should().Equal(target.Id);
    }
}
