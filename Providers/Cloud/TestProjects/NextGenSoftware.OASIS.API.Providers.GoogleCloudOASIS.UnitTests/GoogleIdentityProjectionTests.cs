using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.GoogleCloudOASIS.UnitTests;

[TestClass]
public sealed class GoogleIdentityProjectionTests
{
    private static IHolonBase Record(bool detail,bool empty=false)=>detail
        ?new AvatarDetail{Id=Guid.NewGuid(),Username=empty?null:"owner",Email=empty?null:"owner@example.invalid"}
        :new Avatar{Id=Guid.NewGuid(),Username=empty?null:"owner",Email=empty?null:"owner@example.invalid"};
    private static Dictionary<string,object> Fields(IHolonBase record)=>new(){["id"]=record.Id.ToString(),["data_json"]=OasisJson.Serialize(record)};
    private static Task Read(IHolonBase record,Dictionary<string,object> fields)
        =>(Task)typeof(GoogleCloudOASIS).GetMethod("ReadFirestoreHistoryAsync",BindingFlags.Static|BindingFlags.NonPublic)!.MakeGenericMethod(record.GetType()).Invoke(null,new object[]{fields,record.Id.ToString(),0})!;

    [DataTestMethod]
    [DataRow(false,"username",0)][DataRow(false,"username",1)][DataRow(false,"username",2)]
    [DataRow(false,"email",0)][DataRow(false,"email",1)][DataRow(false,"email",2)]
    [DataRow(true,"username",0)][DataRow(true,"username",1)][DataRow(true,"username",2)]
    [DataRow(true,"email",0)][DataRow(true,"email",1)][DataRow(true,"email",2)]
    public async Task NativeIdentityProjectionCannotContradictCanonicalRecord(bool detail,string field,int corruption)
    {
        var record=Record(detail);var fields=Fields(record);
        fields[field]=corruption==0?"other":corruption==1?(object)42L:field=="username"?"OWNER":"OWNER@EXAMPLE.INVALID";
        await Assert.ThrowsExceptionAsync<InvalidDataException>(()=>Read(record,fields));
    }

    [DataTestMethod][DataRow(false,0)][DataRow(false,1)][DataRow(false,2)][DataRow(true,0)][DataRow(true,1)][DataRow(true,2)]
    public async Task AbsentLegacyAndMatchingIdentityProjectionsRemainReadable(bool detail,int mode)
    {
        var record=Record(detail,mode==2);var fields=Fields(record);
        if(mode!=0){fields["username"]=mode==2?null!:"owner";fields["email"]=mode==2?null!:"owner@example.invalid";}
        await Read(record,fields);
    }
}
