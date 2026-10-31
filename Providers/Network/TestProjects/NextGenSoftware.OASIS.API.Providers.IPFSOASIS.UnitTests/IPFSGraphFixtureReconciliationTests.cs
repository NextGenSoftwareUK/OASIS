using System.Security.Cryptography;
using System.Text.Json;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.Common;
using Xunit;

namespace NextGenSoftware.OASIS.API.Providers.IPFSOASIS.UnitTests;

[Collection("Owned IPFS")]
public class IPFSGraphFixtureReconciliationTests
{
    [Fact,Trait("Category","IPFSGraphFixtureCleanup")]
    public async Task VerifyAndRemoveOnlyTheReviewedGraphSaveFixtureCatalogues()
    {
        var checkout=new DirectoryInfo(AppContext.BaseDirectory);
        while(checkout!=null&&!Directory.Exists(Path.Combine(checkout.FullName,"Scripts","TestHosts")))checkout=checkout.Parent;
        Assert.NotNull(checkout);
        var state=Path.GetDirectoryName(Environment.GetEnvironmentVariable("IPFS_PATH"))!;
        var pending=Path.Combine(state,"graph-ack-cleanup.pending.json");var complete=Path.Combine(state,"graph-ack-cleanup.json");
        Assert.False(File.Exists(pending)||File.Exists(pending+".next")||File.Exists(complete),"Never replay an original cleanup.");
        var planned=new List<(IPFSOASIS Provider,string Mode,byte[] Dna,string Path,string DnaHash,Guid Owner,Guid Root,Guid Child)>();
        var outcomes=new List<object>();
        var priorDna=OASISDNAManager.OASISDNA;var priorPath=OASISDNAManager.OASISDNAPath;
        try
        {
            foreach(var mode in new[]{"async","sync"})
            {
                var raw=await File.ReadAllBytesAsync(Path.Combine(checkout!.FullName,"TestResults","ProviderConformanceCurrent","IPFSOASIS",mode,"report.json"));
                Assert.Equal(mode=="async"?"87CE23CAF815F74ABFD512B9B158599C2D63F98DB2264F52154EC263B8CCC0DD":"91323A556F5BF3013E783028978864C5087D8A6B4F715A2557C563186CBC63D2",Convert.ToHexString(SHA256.HashData(raw)));
                using var report=JsonDocument.Parse(raw);var r=report.RootElement;
                Assert.Equal("graph-save",r.GetProperty("CurrentCase").GetString());Assert.Equal(9,r.GetProperty("Passed").GetArrayLength());
                var owner=r.GetProperty("OwnerId").GetGuid();var root=r.GetProperty("RootId").GetGuid();var child=r.GetProperty("ChildId").GetGuid();
                var path=Path.Combine(state,"conformance-"+mode+"-dna.json");var bytes=await File.ReadAllBytesAsync(path);
                var dna=Newtonsoft.Json.JsonConvert.DeserializeObject<OASISDNA>(System.Text.Encoding.UTF8.GetString(bytes))!;
                var provider=new IPFSOASIS(dna,path);Assert.False((await provider.ActivateProviderAsync()).IsError);
                var catalog=await provider.LoadLookupToJson();Assert.Equal(4,catalog.Count);
                var expected=new HashSet<(string,Guid)>{("avatar",owner),("avatar-detail",owner),("holon",root),("holon",child)};
                Assert.True(expected.SetEquals(catalog.Values.Select(row=>(row.RecordKind,row.Id))));
                var avatar=await provider.LoadAvatarAsync(owner);var detail=await provider.LoadAvatarDetailAsync(owner);
                var parent=await provider.LoadHolonAsync(root,loadChildren:false);var descendant=await provider.LoadHolonAsync(child,loadChildren:false);
                Assert.False(avatar.IsError);Assert.False(detail.IsError);Assert.False(parent.IsError);Assert.False(descendant.IsError);
                var marker="conformance-"+owner.ToString("N");
                Assert.Equal(marker,avatar.Result.Username);Assert.Equal(marker,detail.Result.Username);
                Assert.Equal(marker,parent.Result.Name);Assert.Equal(marker+"-child",descendant.Result.Name);Assert.Equal(root,descendant.Result.ParentHolonId);
                planned.Add((provider,mode,bytes,path,Convert.ToHexString(SHA256.HashData(bytes)),owner,root,child));
            }
            var archive=Path.Combine(state,"Archive");Directory.CreateDirectory(archive);
            foreach(var item in planned){
                await using var original=new FileStream(Path.Combine(archive,"graph-ack-original-"+item.Mode+"-dna.json"),FileMode.CreateNew,FileAccess.Write,FileShare.None);
                await original.WriteAsync(item.Dna);original.Flush(true);
            }
            async Task Progress(bool closed){
                await using(var file=new FileStream(pending+".next",FileMode.CreateNew,FileAccess.Write,FileShare.None)){
                    await JsonSerializer.SerializeAsync(file,new{schema=1,complete=closed,
                        scope="Exact verified owned catalogue fixture maintenance; no shipping delete/historical erasure claim",
                        intents=planned.Select(p=>new{mode=p.Mode,owner=p.Owner,root=p.Root,child=p.Child,dnaSha256=p.DnaHash}),outcomes});file.Flush(true);
                }
                File.Move(pending+".next",pending,true);
            }
            await Progress(false);
            foreach(var item in planned){
                Assert.Equal(item.DnaHash,Convert.ToHexString(SHA256.HashData(await File.ReadAllBytesAsync(item.Path))));
                // Explicit fixture maintenance after exact ownership/payload checks.
                // This does not stand in for the shipping DeleteHolon contract.
                await item.Provider.SaveLookupToFile(new Dictionary<string,HolonResume>());
                Assert.Empty(await item.Provider.LoadLookupToJson());
                Assert.Equal(OASISResultErrorCodes.RecordNotFound,(await item.Provider.LoadAvatarAsync(item.Owner)).ErrorCode);
                Assert.Equal(OASISResultErrorCodes.RecordNotFound,(await item.Provider.LoadAvatarDetailAsync(item.Owner)).ErrorCode);
                Assert.Equal(OASISResultErrorCodes.RecordNotFound,(await item.Provider.LoadHolonAsync(item.Root,loadChildren:false)).ErrorCode);
                Assert.Equal(OASISResultErrorCodes.RecordNotFound,(await item.Provider.LoadHolonAsync(item.Child,loadChildren:false)).ErrorCode);
                outcomes.Add(new{mode=item.Mode,catalogueEmpty=true,allFourPointsAbsent=true});await Progress(false);
            }
            await Progress(true);File.Move(pending,complete);
        }
        finally{OASISDNAManager.OASISDNA=priorDna;OASISDNAManager.OASISDNAPath=priorPath;}
    }
}
