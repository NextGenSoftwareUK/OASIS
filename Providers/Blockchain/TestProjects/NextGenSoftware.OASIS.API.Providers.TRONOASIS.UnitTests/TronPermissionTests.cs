using Google.Protobuf;
using TronNet.Protocol;
using Xunit;

namespace NextGenSoftware.OASIS.API.Providers.TRONOASIS.UnitTests;

public class TronPermissionTests
{
    [Theory]
    [InlineData("valid")] [InlineData("threshold")] [InlineData("mask")] [InlineData("duplicate")] [InlineData("foreign-key")] [InlineData("foreign-account")]
    public void ActivePermissionUsesExactAccountMaskAndUniqueWeightedKeys(string mode)
    {
        var ownerBytes=new byte[21];ownerBytes[0]=0x41;ownerBytes[1]=1;var owner=ByteString.CopyFrom(ownerBytes);
        var signerBytes=(byte[])ownerBytes.Clone();signerBytes[1]=2;var signer=ByteString.CopyFrom(signerBytes);
        var operations=new byte[32];operations[0]=2;
        var permission=new Permission{Id=2,Type=(Permission.Types.PermissionType)2,Threshold=mode=="threshold"?2:1,Operations=ByteString.CopyFrom(mode=="mask"?new byte[32]:operations)};
        permission.Keys.Add(new Key{Address=signer,Weight=1});
        if(mode=="duplicate")permission.Keys.Add(new Key{Address=signer,Weight=1});
        var account=new Account{Address=mode=="foreign-account"?signer:owner};account.ActivePermission.Add(permission);
        if(mode=="valid")TronPermissionVerifier.Verify(account,owner,2,1,new[]{signer});
        else Assert.Throws<System.IO.InvalidDataException>(()=>TronPermissionVerifier.Verify(account,owner,2,1,new[]{mode=="foreign-key"?owner:signer}));
    }
}
