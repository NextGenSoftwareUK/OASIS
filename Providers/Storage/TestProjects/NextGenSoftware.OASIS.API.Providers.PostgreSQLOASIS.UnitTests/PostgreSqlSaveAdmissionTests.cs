using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;

namespace NextGenSoftware.OASIS.API.Providers.PostgreSQLOASIS.UnitTests;

[TestClass]
public class PostgreSqlSaveAdmissionTests
{
    [DataTestMethod][DataRow(0)][DataRow(1)][DataRow(2)]
    public void CanonicalNativeCodecPreservesMetadataValues(int kind)
    {
        IHolonBase source=kind==0?new Avatar():kind==1?new AvatarDetail():new Holon();source.Id=Guid.NewGuid();
        source.MetaData["number"]=42;source.MetaData["text"]="kept";
        var flags=System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic;
        var json=(string)typeof(PostgreSQLOASIS).GetMethod("Serialize",flags)!.Invoke(null,new object[]{source})!;
        var decoded=(IHolonBase)typeof(PostgreSQLOASIS).GetMethod("Deserialize",flags)!.MakeGenericMethod(source.GetType()).Invoke(null,new object[]{json})!;
        Assert.AreEqual(source.Id,decoded.Id);Assert.AreEqual(42,Convert.ToInt32(decoded.MetaData["number"]));Assert.AreEqual("kept",decoded.MetaData["text"].ToString());
    }

    [DataTestMethod][DataRow(0,false)][DataRow(0,true)][DataRow(1,false)][DataRow(1,true)][DataRow(2,false)][DataRow(2,true)]
    public void AcknowledgedPublicationPreservesCallerEditsAndRecordEvidence(int kind,bool edit)
    {
        if(kind==0)Check(new Avatar{IsNewHolon=true},edit);
        else if(kind==1)Check(new AvatarDetail{IsNewHolon=true},edit);
        else Check(new Holon{IsNewHolon=true},edit);
    }
    private static void Check<T>(T source,bool edit) where T:class,IHolonBase
    {
        var provider=new PostgreSQLOASIS("Unused=true");
        var publication=NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.StoredRecordPublicationSnapshot<T>.Capture(source);
        var stored=publication.Proposal;stored.Id=Guid.NewGuid();
        if(edit)source.MetaData["edit"]="preserve";
        var method=typeof(PostgreSQLOASIS).GetMethod("PublishAcknowledgedIdentity",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic)!.MakeGenericMethod(typeof(T));
        if(edit){
            var error=Assert.ThrowsException<System.Reflection.TargetInvocationException>(()=>method.Invoke(provider,new object[]{source,stored,publication}));
            var cause=error.InnerException as PostgreSqlWriteReconciliationException;Assert.IsNotNull(cause);Assert.AreEqual(stored.Id,cause.RecordId);
            Assert.IsTrue(Core.Helpers.NativeMutationReconciliation.IsRequired(cause));Assert.AreEqual(Guid.Empty,source.Id);Assert.IsTrue(source.IsNewHolon);Assert.AreEqual("preserve",source.MetaData["edit"]);
        }else{
            method.Invoke(provider,new object[]{source,stored,publication});Assert.AreEqual(stored.Id,source.Id);Assert.IsFalse(source.IsNewHolon);
            Assert.AreEqual(stored.Id.ToString(),source.ProviderUniqueStorageKey[Core.Enums.ProviderType.PostgreSQLOASIS]);
        }
    }

    [DataTestMethod]
    [DataRow(0,false)][DataRow(0,true)][DataRow(1,false)][DataRow(1,true)][DataRow(2,false)][DataRow(2,true)]
    public async Task InvalidConnectionCannotPublishIdentityOrProviderKey(int kind,bool synchronous)
    {
        var provider=new PostgreSQLOASIS("UnsupportedConnectionKeyword=true");
        IHolonBase source=kind==0?new Avatar():kind==1?new AvatarDetail():new Holon();
        source.IsNewHolon=true;source.MetaData["keep"]="original";
        if(kind==0){var result=synchronous?provider.SaveAvatar((IAvatar)source):await provider.SaveAvatarAsync((IAvatar)source);Assert.IsTrue(result.IsError);Assert.IsNotNull(result.Exception);}
        else if(kind==1){var result=synchronous?provider.SaveAvatarDetail((IAvatarDetail)source):await provider.SaveAvatarDetailAsync((IAvatarDetail)source);Assert.IsTrue(result.IsError);Assert.IsNotNull(result.Exception);}
        else{var result=synchronous?provider.SaveHolon((IHolon)source,saveChildren:false):await provider.SaveHolonAsync((IHolon)source,saveChildren:false);Assert.IsTrue(result.IsError);Assert.IsNotNull(result.Exception);}
        Assert.AreEqual(Guid.Empty,source.Id);Assert.IsTrue(source.IsNewHolon);Assert.AreEqual(0,source.Version);
        Assert.IsFalse(source.ProviderUniqueStorageKey?.ContainsKey(Core.Enums.ProviderType.PostgreSQLOASIS)==true);
        Assert.AreEqual("original",source.MetaData["keep"]);
    }
}
