using System.Security.Cryptography;
using System.Text.Json;
using FluentAssertions;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Objects.Search;
using Xunit;

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.IntegrationTests;

public partial class SQLLiteDBOASISIntegrationTests
{
    [Theory][InlineData(false)][InlineData(true)]
    public async Task EmptyExportOwnerRejectsBeforeActivation(bool synchronous)
    {
        using var provider=new SQLLiteDBOASIS(TestConnectionString);
        var result=synchronous?provider.ExportAllDataForAvatarById(Guid.Empty):await provider.ExportAllDataForAvatarByIdAsync(Guid.Empty);
        result.IsError.Should().BeTrue();result.Result.Should().BeNull();
        result.Exception.Should().BeOfType<ArgumentException>().Which.ParamName.Should().Be("avatarId");
        provider.IsProviderActivated.Should().BeFalse();File.Exists(_databasePath).Should().BeFalse();
    }

    public static IEnumerable<object[]> ScopedGraphCases()
    {
        foreach(var synchronous in new[]{false,true})foreach(var version in new[]{0,3})foreach(var path in new[]{"search","export","flat-search"})
            yield return new object[]{synchronous,version,path};
    }

    [Theory][MemberData(nameof(ScopedGraphCases))]
    public async Task ScopedQueriesHideForeignDescendantsWithoutReplacingMembership(bool synchronous,int version,string path)
    {
        var pending=Path.Combine(Path.GetDirectoryName(_databasePath)!,"scoped-visibility.pending.json");
        File.Exists(pending).Should().BeFalse("original native uncertainty must be reconciled, not overwritten");
        var actor=Guid.NewGuid();var name="owned-scope-"+actor.ToString("N");
        var root=new Holon{Id=Guid.NewGuid(),CreatedByAvatarId=actor,Name=name,Version=3,MetaData=new(){{"scope",name}}};
        var owned=new Holon{Id=Guid.NewGuid(),ParentHolonId=root.Id,CreatedByAvatarId=actor,Version=7};
        var hidden=new Holon{Id=Guid.NewGuid(),ParentHolonId=root.Id,CreatedByAvatarId=Guid.NewGuid(),Version=8};
        var foreignPublic=new Holon{Id=Guid.NewGuid(),ParentHolonId=root.Id,CreatedByAvatarId=Guid.NewGuid(),IsPublic=true,Version=9};
        var leaf=new Holon{Id=Guid.NewGuid(),ParentHolonId=hidden.Id,CreatedByAvatarId=actor,Version=10};
        var rows=new[]{root,owned,hidden,foreignPublic,leaf};var views=new List<Holon>();
        var intentBytes=JsonSerializer.SerializeToUtf8Bytes(new{actor,ids=rows.Select(row=>row.Id),synchronous,version,path,state="before-owned-scoped-graph-save"});
        await using(var intent=new FileStream(pending,FileMode.CreateNew,FileAccess.Write,FileShare.None,4096,FileOptions.WriteThrough)){
            await intent.WriteAsync(intentBytes);intent.Flush(true);
        }
        var baselinePath=pending+".read-baseline.json";byte[] baselineBytes=null!;
        using(var provider=new SQLLiteDBOASIS(TestConnectionString)){
            (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
            (await provider.SaveAvatarAsync(new Avatar{Id=actor,Username=name,Email=name+"@example.invalid"})).IsError.Should().BeFalse();
            foreach(var row in rows)(await provider.SaveHolonAsync(row,saveChildren:false)).IsError.Should().BeFalse();
            var complete=await provider.LoadHolonAsync(root.Id,loadChildrenFromProvider:true);
            complete.IsError.Should().BeFalse(complete.Exception?.ToString());complete.Result.Children.Should().HaveCount(3);
            complete.Result.Children.Single(row=>row.Id==hidden.Id).Children.Single().Id.Should().Be(leaf.Id);
            string Fingerprint()=>string.Join("|",new[]{_databasePath,_databasePath+"-wal"}.Select(file=>File.Exists(file)?Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(file))):"absent"));
            var before=Fingerprint();
            baselineBytes=JsonSerializer.SerializeToUtf8Bytes(new{actor,root.Id,fingerprint=before,state="acknowledged-originals-before-query-and-rejected-save"});
            await using(var baseline=new FileStream(baselinePath,FileMode.CreateNew,FileAccess.Write,FileShare.None,4096,FileOptions.WriteThrough)){
                await baseline.WriteAsync(baselineBytes);baseline.Flush(true);
            }
            if(path=="export"){
                var exports=new[]{
                    synchronous?provider.ExportAllDataForAvatarById(actor,version):await provider.ExportAllDataForAvatarByIdAsync(actor,version),
                    synchronous?provider.ExportAllDataForAvatarByUsername(name,version):await provider.ExportAllDataForAvatarByUsernameAsync(name,version),
                    synchronous?provider.ExportAllDataForAvatarByEmail(name+"@example.invalid",version):await provider.ExportAllDataForAvatarByEmailAsync(name+"@example.invalid",version)};
                foreach(var exported in exports){exported.IsError.Should().BeFalse(exported.Exception?.ToString());views.Add((Holon)exported.Result.Single(row=>row.Id==root.Id));}
            }else{
                var query=new SearchParams{SearchOnlyForCurrentAvatar=true,AvatarId=actor,FilterByMetaData=new(){{"scope",name}}};
                var selected=synchronous?provider.Search(query,loadChildren:path!="flat-search",version:version):await provider.SearchAsync(query,loadChildren:path!="flat-search",version:version);
                selected.IsError.Should().BeFalse(selected.Exception?.ToString());selected.LoadedCount.Should().Be(1);views.Add((Holon)selected.Result.SearchResultHolons.Single());
            }
            foreach(var view in views.Where(view=>path!="flat-search"&&view.Children.Count==1&&view.Children.Single().Id==owned.Id)){
                view.AcceptChanges();var rejected=synchronous?provider.SaveHolon(view):await provider.SaveHolonAsync(view);
                rejected.IsError.Should().BeTrue();rejected.IsSaved.Should().BeFalse();rejected.Exception.Should().BeOfType<InvalidOperationException>();
            }
            Fingerprint().Should().Be(before,"scoped reads/rejected saves must preserve native database and WAL bytes");
        }
        // Known successful native writes + unchanged bytes permit the existing owned fixture teardown.
        // Failures above retain this intent and the SAME database through fixture Dispose.
        SHA256.HashData(await File.ReadAllBytesAsync(baselinePath)).Should().Equal(SHA256.HashData(baselineBytes));
        SHA256.HashData(await File.ReadAllBytesAsync(pending)).Should().Equal(SHA256.HashData(intentBytes),"only the unchanged original owned intent may be retired");
        File.Delete(baselinePath);
        File.Delete(pending);
        foreach(var view in views){
            view.Children.Select(row=>row.Id).Should().Equal(path=="flat-search"?Array.Empty<Guid>():new[]{owned.Id});
            if(path!="flat-search")view.Children.Single().Version.Should().Be(7);
        }
    }
}
