using System.Security.Cryptography;
using System.Text.Json;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.Common;
using Xunit;

namespace NextGenSoftware.OASIS.API.Providers.IPFSOASIS.UnitTests;

[Collection("Owned IPFS")]
public class IPFSParentFixtureReconciliationTests
{
    [Fact,Trait("Category","IPFSParentFixtureCleanup")]
    public async Task VerifyAndRemoveOnlyTheReviewedDeleteFixtureCatalogues()
    {
        var checkout=new DirectoryInfo(AppContext.BaseDirectory);
        while(checkout!=null&&!Directory.Exists(Path.Combine(checkout.FullName,"Scripts","TestHosts")))checkout=checkout.Parent;
        Assert.NotNull(checkout);
        var state=Path.GetDirectoryName(Environment.GetEnvironmentVariable("IPFS_PATH"))!;
        var pending=Path.Combine(state,"parent-ack-cleanup.pending.json");var complete=Path.Combine(state,"parent-ack-cleanup.json");
        Assert.False(File.Exists(pending)||File.Exists(pending+".next")||File.Exists(complete),"Never replay an original cleanup.");
        var planned=new List<(IPFSOASIS Provider,string Mode,byte[] Dna,string Path,string DnaHash,Guid Owner,Guid Root,Guid Child)>();
        var outcomes=new List<object>();
        var remaining=new Dictionary<string,Guid[]>();
        var priorDna=OASISDNAManager.OASISDNA;var priorPath=OASISDNAManager.OASISDNAPath;
        try
        {
            foreach(var mode in new[]{"async","sync"})
            {
                var raw=await File.ReadAllBytesAsync(Path.Combine(checkout!.FullName,"TestResults","ProviderConformanceCurrent","IPFSOASIS",mode,"report.json"));
                Assert.Equal(mode=="async"?"A4CCB2842560B240A4EE98D1C24A21AE23F5515E2928FB8FAC8C5874524EF972":"9F9441BF259323E84C3B946A975BECF8E0B7731CC5A0765D68E4A2E7A196F1B0",Convert.ToHexString(SHA256.HashData(raw)));
                using var report=JsonDocument.Parse(raw);var r=report.RootElement;
                Assert.Equal("holon-delete-"+r.GetProperty("ChildId").GetGuid().ToString("N"),r.GetProperty("CurrentCase").GetString());Assert.Equal(18,r.GetProperty("Passed").GetArrayLength());
                var owner=r.GetProperty("OwnerId").GetGuid();var root=r.GetProperty("RootId").GetGuid();var child=r.GetProperty("ChildId").GetGuid();
                var path=Path.Combine(state,"conformance-"+mode+"-dna.json");var bytes=await File.ReadAllBytesAsync(path);
                var dna=Newtonsoft.Json.JsonConvert.DeserializeObject<OASISDNA>(System.Text.Encoding.UTF8.GetString(bytes))!;
                var provider=new IPFSOASIS(dna,path);Assert.False((await provider.ActivateProviderAsync()).IsError);
                var catalog=await provider.LoadLookupToJson();Assert.Equal(7,catalog.Count);
                var expected=new HashSet<(string,Guid)>{("avatar",owner),("avatar-detail",owner),("holon",root),("holon",child)};
                var batch=r.GetProperty("BatchIds").EnumerateArray().Select(x=>x.GetGuid()).ToArray();
                var imported=r.GetProperty("ImportId").GetGuid();
                foreach(var id in batch.Append(imported))expected.Add(("holon",id));
                Assert.True(expected.SetEquals(catalog.Values.Select(row=>(row.RecordKind,row.Id))));
                var avatar=await provider.LoadAvatarAsync(owner);var detail=await provider.LoadAvatarDetailAsync(owner);
                var parent=await provider.LoadHolonAsync(root,loadChildren:false);var descendant=await provider.LoadHolonAsync(child,loadChildren:false);
                Assert.False(avatar.IsError);Assert.False(detail.IsError);Assert.False(parent.IsError);Assert.False(descendant.IsError);
                var marker="conformance-"+owner.ToString("N");
                Assert.Equal(marker,avatar.Result.Username);Assert.Equal(marker,detail.Result.Username);
                Assert.Equal(marker+"-updated",parent.Result.Name);Assert.Equal(marker+"-child",descendant.Result.Name);Assert.Equal(root,descendant.Result.ParentHolonId);
                Assert.True(descendant.Result.IsDeleted);Assert.False(descendant.Result.IsActive);
                Assert.Equal(owner,parent.Result.CreatedByAvatarId);Assert.Equal(owner,descendant.Result.CreatedByAvatarId);
                foreach(var id in batch.Append(imported)){
                    var row=await provider.LoadHolonAsync(id,loadChildren:false);Assert.False(row.IsError);
                    Assert.Equal(owner,row.Result.CreatedByAvatarId);Assert.False(row.Result.IsDeleted);
                    Assert.Equal(marker+(id==imported?"-import":"-batch"),row.Result.Name);
                }
                planned.Add((provider,mode,bytes,path,Convert.ToHexString(SHA256.HashData(bytes)),owner,root,child));
            }
            foreach(var mode in new[]{"async","sync"}){
                using var r=JsonDocument.Parse(await File.ReadAllBytesAsync(Path.Combine(checkout!.FullName,"TestResults","ProviderConformanceCurrent","IPFSOASIS",mode,"report.json")));
                remaining[mode]=r.RootElement.GetProperty("BatchIds").EnumerateArray().Select(x=>x.GetGuid()).Append(r.RootElement.GetProperty("ImportId").GetGuid()).ToArray();
            }
            var archive=Path.Combine(state,"Archive");Directory.CreateDirectory(archive);
            foreach(var item in planned){
                await using var original=new FileStream(Path.Combine(archive,"parent-ack-original-"+item.Mode+"-dna.json"),FileMode.CreateNew,FileAccess.Write,FileShare.None);
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
                foreach(var id in remaining[item.Mode])Assert.Equal(OASISResultErrorCodes.RecordNotFound,(await item.Provider.LoadHolonAsync(id,loadChildren:false)).ErrorCode);
                outcomes.Add(new{mode=item.Mode,catalogueEmpty=true,allSevenPointsAbsent=true});await Progress(false);
            }
            await Progress(true);File.Move(pending,complete);
        }
        finally{OASISDNAManager.OASISDNA=priorDna;OASISDNAManager.OASISDNAPath=priorPath;}
    }
}
