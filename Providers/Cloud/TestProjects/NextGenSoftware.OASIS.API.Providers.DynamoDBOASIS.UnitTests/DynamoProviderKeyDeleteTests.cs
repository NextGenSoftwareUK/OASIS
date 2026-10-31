using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.DynamoDBOASIS.UnitTests;

[TestClass]
public sealed class DynamoProviderKeyDeleteTests
{
    private sealed class Routes():DynamoDBOASIS("owned","owned","us-east-1","http://127.0.0.1:18001")
    {
        public int IdCalls,UsernameCalls;public Guid SelectedId;public bool SelectedSoft;
        public override Task<OASISResult<bool>> DeleteAvatarAsync(Guid id,bool softDelete=true)
        {IdCalls++;SelectedId=id;SelectedSoft=softDelete;return Task.FromResult(new OASISResult<bool>{Result=true,IsDeleted=true,DeletedCount=1});}
        public override Task<OASISResult<bool>> DeleteAvatarByUsernameAsync(string username,bool softDelete=true)
        {UsernameCalls++;throw new InvalidOperationException("Provider keys must never dispatch as usernames.");}
    }

    public static IEnumerable<object[]> Cases()
    {
        foreach(var key in new string?[]{null,""," ","owned-user",Guid.Empty.ToString(),Guid.NewGuid().ToString()})
            foreach(var sync in new[]{false,true})foreach(var soft in new[]{false,true})yield return new object[]{key!,sync,soft};
    }

    [DataTestMethod][DynamicData(nameof(Cases),DynamicDataSourceType.Method)]
    public async Task ProviderKeyDeletionNeverReinterpretsUsername(string? key,bool sync,bool soft)
    {
        var provider=new Routes();OASISResult<bool>? result=null;Exception? escaped=null;
        try{result=sync?provider.DeleteAvatar(key!,soft):await provider.DeleteAvatarAsync(key!,soft);}catch(Exception cause){escaped=cause;}
        Assert.IsNull(escaped,"Validation must return an OASISResult, never dispatch to username deletion.");
        Assert.IsNotNull(result);Assert.AreEqual(0,provider.UsernameCalls);
        var valid=Guid.TryParse(key,out var id)&&id!=Guid.Empty;
        Assert.AreEqual(valid?1:0,provider.IdCalls);
        if(valid){Assert.IsFalse(result.IsError);Assert.IsTrue(result.Result);Assert.AreEqual(id,provider.SelectedId);Assert.AreEqual(soft,provider.SelectedSoft);}
        else{Assert.IsTrue(result.IsError);Assert.IsInstanceOfType<ArgumentException>(result.Exception);Assert.IsFalse(result.Result);Assert.IsFalse(result.IsDeleted);Assert.AreEqual(0,result.DeletedCount);}
    }
}
