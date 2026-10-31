using Google.Protobuf;
using TronNet.Protocol;
using Xunit;

namespace NextGenSoftware.OASIS.API.Providers.TRONOASIS.UnitTests;

public class TronPermissionTests
{
    [Theory]
    [InlineData("valid")] [InlineData("foreign")] [InlineData("duplicate")] [InlineData("malformed-explicit")]
    public void AbsentOwnerMessageUsesProtocolDefaultButMalformedExplicitPermissionDoesNot(string mode)
    {
        var bytes=new byte[21];bytes[0]=0x41;bytes[1]=1;var owner=ByteString.CopyFrom(bytes);
        var account=new Account{Address=owner};
        if(mode=="malformed-explicit")account.OwnerPermission=new Permission();
        bytes[1]=2;var foreign=ByteString.CopyFrom(bytes);
        var signers=mode=="duplicate"?new[]{owner,owner}:new[]{mode=="foreign"?foreign:owner};
        if(mode=="valid")TronPermissionVerifier.Verify(account,owner,0,1,signers);
        else Assert.Throws<System.IO.InvalidDataException>(()=>TronPermissionVerifier.Verify(account,owner,0,1,signers));
    }
    [Theory]
    [InlineData("valid")] [InlineData("threshold")] [InlineData("mask")] [InlineData("duplicate")] [InlineData("foreign-key")] [InlineData("foreign-account")]
    [InlineData("duplicate-signer")] [InlineData("zero-weight")] [InlineData("wrong-id")] [InlineData("missing")]
    [InlineData("witness")] [InlineData("zero-threshold")] [InlineData("malformed-mask")]
    public void ActivePermissionUsesExactAccountMaskAndUniqueWeightedKeys(string mode)
    {
        var ownerBytes=new byte[21];ownerBytes[0]=0x41;ownerBytes[1]=1;var owner=ByteString.CopyFrom(ownerBytes);
        var signerBytes=(byte[])ownerBytes.Clone();signerBytes[1]=2;var signer=ByteString.CopyFrom(signerBytes);
        var operations=new byte[32];operations[0]=2;
        var permission=new Permission{Id=2,Type=(Permission.Types.PermissionType)2,Threshold=mode=="threshold"?2:1,Operations=ByteString.CopyFrom(mode=="mask"?new byte[32]:operations)};
        permission.Keys.Add(new Key{Address=signer,Weight=1});
        if(mode=="zero-weight")permission.Keys[0].Weight=0;
        if(mode=="wrong-id")permission.Id=3;
        if(mode=="witness")permission.Type=(Permission.Types.PermissionType)1;
        if(mode=="zero-threshold")permission.Threshold=0;
        if(mode=="malformed-mask")permission.Operations=ByteString.CopyFrom(new byte[1]);
        if(mode=="duplicate")permission.Keys.Add(new Key{Address=signer,Weight=1});
        var account=new Account{Address=mode=="foreign-account"?signer:owner};if(mode!="missing")account.ActivePermission.Add(permission);
        if(mode=="valid")TronPermissionVerifier.Verify(account,owner,2,1,new[]{signer});
        else Assert.Throws<System.IO.InvalidDataException>(()=>TronPermissionVerifier.Verify(account,owner,2,1,mode=="duplicate-signer"?new[]{signer,signer}:new[]{mode=="foreign-key"?owner:signer}));
    }
}
