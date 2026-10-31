using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.GoogleCloudOASIS.UnitTests;

[TestClass]
public sealed class GoogleLifecycleGuardTests
{
    [DataTestMethod][DataRow(0,false)][DataRow(1,false)][DataRow(2,false)][DataRow(0,true)][DataRow(1,true)][DataRow(2,true)]
    public async Task ActiveStagingCannotReviveACommittedTombstone(int kind,bool history)
    {
        var id=Guid.NewGuid();IHolonBase current=kind==0?new Avatar{Id=id}:kind==1?new AvatarDetail{Id=id}:new Holon{Id=id};
        IHolonBase active=kind==0?new Avatar{Id=id}:kind==1?new AvatarDetail{Id=id}:new Holon{Id=id};
        current.IsActive=false;current.DeletedDate=DateTime.UtcNow;
        var prior=new Dictionary<string,object>{["id"]=id.ToString(),["data_json"]=OasisJson.Serialize(current),["isActive"]=false,["isDeleted"]=true};
        var method=typeof(GoogleCloudOASIS).GetMethod("StageFirestoreDocumentAsync",BindingFlags.Static|BindingFlags.NonPublic)!.MakeGenericMethod(current.GetType());
        var task=(Task)method.Invoke(null,new object[]{active,prior,id.ToString(),history})!;
        await Assert.ThrowsExceptionAsync<StorageLifecycleConflictException>(async()=>await task);
        Assert.IsFalse(((IHolonBase)active).IsDeleted);
    }

    [DataTestMethod][DataRow(0)][DataRow(1)][DataRow(2)]
    public async Task TombstoneGuardMustValidateCanonicalIdentityBeforeRejecting(int kind)
    {
        var id=Guid.NewGuid();
        IHolonBase proposed=kind==0?new Avatar{Id=id}:kind==1?new AvatarDetail{Id=id}:new Holon{Id=id};
        IHolonBase unrelated=kind==0?new Avatar{Id=Guid.NewGuid()}:kind==1?new AvatarDetail{Id=Guid.NewGuid()}:new Holon{Id=Guid.NewGuid()};
        unrelated.IsActive=false;unrelated.DeletedDate=DateTime.UtcNow;
        var prior=new Dictionary<string,object>{["id"]=id.ToString(),["data_json"]=OasisJson.Serialize(unrelated),["isActive"]=false,["isDeleted"]=true};
        var method=typeof(GoogleCloudOASIS).GetMethod("StageFirestoreDocumentAsync",BindingFlags.Static|BindingFlags.NonPublic)!.MakeGenericMethod(proposed.GetType());
        var task=(Task)method.Invoke(null,new object[]{proposed,prior,id.ToString(),false})!;
        await Assert.ThrowsExceptionAsync<System.IO.InvalidDataException>(async()=>await task);
    }
}
