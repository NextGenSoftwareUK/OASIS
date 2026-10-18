using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Managers.OASISHyperDrive;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.Utilities;

namespace NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.UnitTests;

[TestClass]
public sealed class VersionBoundaryTests
{
    [DataTestMethod]
    [DataRow(false)] [DataRow(true)]
    public async Task ExhaustedVersionCannotFailOverAndCreateFreshRecordElsewhere(bool synchronous)
    {
        var primaryStore=new InMemoryBackend();var secondaryStore=new InMemoryBackend();
        var primary=new TestProvider(primaryStore){IsVersionControlEnabled=true};var secondary=new TestProvider(secondaryStore);
        secondary.ProviderType=new EnumValue<ProviderType>(ProviderType.LocalFileOASIS);
        var source=new Holon{Id=Guid.NewGuid(),Version=int.MaxValue};Assert.IsFalse((await primary.SaveHolonAsync(source,saveChildren:false)).IsError);
        source.Name="must-not-save-elsewhere";
        var dna=new OASISDNA();dna.OASIS.HyperDriveMode=HyperDriveModes.V2;
        dna.OASIS.StorageProviders=new StorageProviderSettings{LogSwitchingProviders=false};dna.OASIS.SubscriptionConfig.PlanType="Gold";
        var manager=new ProviderManager(null,dna){IsAutoFailOverEnabled=true,IsAutoLoadBalanceEnabled=false,IsAutoReplicationEnabled=false};
        manager.RegisterProvider(primary);manager.RegisterProvider(secondary);
        Assert.IsFalse(manager.SetAndReplaceAutoFailOverListForProviders(new[]{primary.ProviderType,secondary.ProviderType}).IsError);
        var router=new OASISHyperDrive(manager);
        var before=primaryStore.Items.OrderBy(row=>row.Key).ToArray();var writes=primaryStore.PutCalls;var caller=OasisJson.Serialize(source);
        var request=new StorageOperationRequest{Operation="SaveHolon",Payload=source,SaveChildren=false,PreferredProvider=primary.ProviderType.Value};
        var rejected=synchronous?router.RouteRequest<IHolon>(request):await router.RouteRequestAsync<IHolon>(request);
        Assert.IsTrue(rejected.IsError,"Version exhaustion must be terminal, not a fresh save on a different provider.");
        Assert.IsInstanceOfType(rejected.Exception,typeof(System.IO.InvalidDataException));
        Assert.AreEqual(0,secondaryStore.PutCalls);Assert.AreEqual(0,secondaryStore.Items.Count);Assert.AreEqual(writes,primaryStore.PutCalls);
        CollectionAssert.AreEqual(before,primaryStore.Items.OrderBy(row=>row.Key).ToArray());Assert.AreEqual(caller,OasisJson.Serialize(source));
    }

    [DataTestMethod]
    [DataRow(false,0,false)] [DataRow(true,0,false)] [DataRow(false,0,true)] [DataRow(true,0,true)]
    [DataRow(false,1,false)] [DataRow(true,1,false)] [DataRow(false,1,true)] [DataRow(true,1,true)]
    [DataRow(false,2,false)] [DataRow(true,2,false)] [DataRow(false,2,true)] [DataRow(true,2,true)]
    public async Task LastRepresentableVersionSucceedsButOverflowCannotArchiveWriteOrPublish(bool synchronous,int kind,bool history)
    {
        var backend=new InMemoryBackend();var provider=new TestProvider(backend){IsVersionControlEnabled=history};
        var id=Guid.NewGuid();
        IHolonBase source=kind==0?new Avatar{Id=id,Username="version-boundary-"+id.ToString("N")}:
            kind==1?new AvatarDetail{Id=id,XP=7}:new Holon{Id=id};
        source.MetaData=new(){{"version-boundary","initial"}};source.Version=int.MaxValue-1;
        async Task<(bool Error,bool Saved,IHolonBase Value,Exception Failure)> Save(){
            if(kind==0){var r=synchronous?provider.SaveAvatar((IAvatar)source):await provider.SaveAvatarAsync((IAvatar)source);return(r.IsError,r.IsSaved,r.Result,r.Exception);}
            if(kind==1){var r=synchronous?provider.SaveAvatarDetail((IAvatarDetail)source):await provider.SaveAvatarDetailAsync((IAvatarDetail)source);return(r.IsError,r.IsSaved,r.Result,r.Exception);}
            var result=synchronous?provider.SaveHolon((IHolon)source,saveChildren:false):await provider.SaveHolonAsync((IHolon)source,saveChildren:false);
            return(result.IsError,result.IsSaved,result.Result,result.Exception);
        }
        var initial=await Save();Assert.IsFalse(initial.Error);Assert.IsTrue(initial.Saved);
        var initialVersionId=source.VersionId;
        source.MetaData["version-boundary"]="last-valid";var last=await Save();Assert.IsFalse(last.Error);Assert.IsTrue(last.Saved);
        Assert.AreEqual(int.MaxValue,source.Version);Assert.AreEqual(initialVersionId,source.PreviousVersionId);
        Assert.AreNotEqual(initialVersionId,source.VersionId);
        var prefix=kind==0?"avatar/":kind==1?"avatardetail/":"holon/";
        var currentKey="oasis/"+prefix+id.ToString("N");
        var expectedHistory="oasis/version/"+prefix+id.ToString("N")+"/"+(int.MaxValue-1).ToString("D10");
        Assert.AreEqual(history,backend.Items.ContainsKey(expectedHistory));
        source.MetaData["version-boundary"]="must-not-persist";source.Version=0;source.VersionId=Guid.Empty;source.PreviousVersionId=Guid.Empty;
        var caller=OasisJson.Serialize(source);var before=backend.Items.OrderBy(row=>row.Key).ToArray();var writes=backend.PutCalls;
        var rejected=await Save();Assert.IsTrue(rejected.Error);Assert.IsFalse(rejected.Saved);Assert.IsNull(rejected.Value);
        Assert.IsInstanceOfType(rejected.Failure,typeof(System.IO.InvalidDataException));
        Assert.IsInstanceOfType(rejected.Failure.InnerException,typeof(OverflowException));
        Assert.AreEqual(writes,backend.PutCalls,"Overflow must reject before even a history write attempt.");
        Assert.AreEqual(caller,OasisJson.Serialize(source));CollectionAssert.AreEqual(before,backend.Items.OrderBy(row=>row.Key).ToArray());
        var raw=System.Text.Json.JsonDocument.Parse(backend.Items[currentKey]);
        using(raw){Assert.AreEqual(int.MaxValue,raw.RootElement.GetProperty("Version").GetInt32());Assert.AreEqual("last-valid",raw.RootElement.GetProperty("MetaData").GetProperty("version-boundary").GetString());}
    }
}
