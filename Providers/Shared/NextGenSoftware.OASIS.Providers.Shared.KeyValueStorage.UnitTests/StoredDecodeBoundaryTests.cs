using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.UnitTests;
[TestClass]
public class StoredDecodeBoundaryTests
{
    [DataTestMethod]
    [DataRow(false,0)] [DataRow(true,0)] [DataRow(false,1)] [DataRow(true,1)] [DataRow(false,2)] [DataRow(true,2)]
    public async Task MalformedStoredRecordIsTerminalAndRetainsJsonCause(bool synchronous,int kind)
    {
        var backend=new InMemoryBackend();var provider=new TestProvider(backend);var id=Guid.NewGuid();
        backend.Items["oasis/"+(kind==0 ? "avatar/":kind==1 ? "avatardetail/":"holon/")+id.ToString("N")]="{invalid stored JSON";
        Exception failure;
        if(kind==0){var result=synchronous ? provider.LoadAvatar(id):await provider.LoadAvatarAsync(id);Assert.IsTrue(result.IsError);Assert.IsNull(result.Result);failure=result.Exception;}
        else if(kind==1){var result=synchronous ? provider.LoadAvatarDetail(id):await provider.LoadAvatarDetailAsync(id);Assert.IsTrue(result.IsError);Assert.IsNull(result.Result);failure=result.Exception;}
        else {var result=synchronous ? provider.LoadHolon(id,loadChildren:false):await provider.LoadHolonAsync(id,loadChildren:false);Assert.IsTrue(result.IsError);Assert.IsNull(result.Result);failure=result.Exception;}
        Assert.IsInstanceOfType(failure,typeof(System.IO.InvalidDataException));Assert.IsInstanceOfType(failure.InnerException,typeof(Newtonsoft.Json.JsonException));
    }
}
