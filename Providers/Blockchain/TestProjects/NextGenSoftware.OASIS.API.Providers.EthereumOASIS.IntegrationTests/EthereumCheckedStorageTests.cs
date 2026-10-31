using System.Numerics;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Nethereum.Hex.HexTypes;
using Nethereum.Util;
using Nethereum.Web3;
using Nethereum.Web3.Accounts;
using NextGenSoftware.OASIS.API.Providers.EthereumOASIS.ContractDefinition;

namespace NextGenSoftware.OASIS.API.Providers.EthereumOASIS.IntegrationTests;

public partial class EthereumOASISIntegrationTests
{
    [DataTestMethod]
    [DataRow("avatar")]
    [DataRow("detail")]
    [DataRow("holon")]
    public async Task CheckedNativeStorageRejectsStalePresenceHashAndForeignIdentity(string kind)
    {
        var (provider,address)=await CreateActivatedProviderAsync();
        try
        {
            var web3=new Web3(new Account(PrivateKey,ChainId),Host);
            var service=new NextGenSoftwareOASISService(web3,address);
            var id=new BigInteger(712);
            var identity=Guid.NewGuid().ToString();
            async Task<string> Read()=>kind switch {
                "avatar"=>(await service.GetAvatarByIdQueryAsync(id)).ReturnValue1.Info,
                "detail"=>(await service.GetAvatarDetailByIdQueryAsync(id)).ReturnValue1.Info,
                _=>(await service.GetHolonByIdQueryAsync(id)).ReturnValue1.Info
            };
            async Task Send(bool present,byte[] hash,string fullIdentity,string info,string rejection=null,Web3 signer=null)
            {
                CheckedStorageFunction request=kind switch {
                    "avatar"=>new CreateAvatarCheckedFunction(),
                    "detail"=>new CreateAvatarDetailCheckedFunction(),
                    _=>new CreateHolonCheckedFunction()
                };
                request.EntityId=id;request.Identity=fullIdentity;request.ExpectedPresent=present;
                request.ExpectedInfoHash=hash;request.Info=info;request.Gas=new HexBigInteger(3000000);
                var handler=(signer??web3).Eth.GetContractHandler(address);
                async Task<Nethereum.RPC.Eth.DTOs.TransactionReceipt> Dispatch()=>request switch {
                    CreateAvatarCheckedFunction avatar=>await handler.SendRequestAndWaitForReceiptAsync(avatar),
                    CreateAvatarDetailCheckedFunction detail=>await handler.SendRequestAndWaitForReceiptAsync(detail),
                    CreateHolonCheckedFunction holon=>await handler.SendRequestAndWaitForReceiptAsync(holon),
                    _=>throw new InvalidOperationException()
                };
                if(rejection!=null){
                    var error=await Assert.ThrowsExceptionAsync<Nethereum.JsonRpc.Client.RpcResponseException>(async()=>{await Dispatch();});
                    StringAssert.Contains(error.Message,"reverted with reason string '"+rejection+"'");
                }else Assert.AreEqual(BigInteger.One,(await Dispatch()).Status.Value);
            }
            var first=Sha3Keccack.Current.CalculateHash(Encoding.UTF8.GetBytes("ciphertext-one"));
            await Send(true,new byte[32],identity,"must-not-create","OASIS presence changed");
            await Send(false,first,identity,"invalid-absent-hash","OASIS absent hash required");
            await Send(false,new byte[32],identity,"ciphertext-one");
            await Send(false,first,identity,"duplicate-create","OASIS presence changed");
            await Send(true,first,Guid.NewGuid().ToString(),"foreign-identity","OASIS identity collision");
            Assert.AreEqual("ciphertext-one",await Read());
            await Send(true,first,identity,"ciphertext-two");
            await Send(true,first,identity,"stale-history","OASIS record changed");
            Assert.AreEqual("ciphertext-two",await Read());
            var other=new Account("0x59c6995e998f97a5a0044966f0945389dc9e86dae88c7a8412f4603b6b78690d",ChainId);
            var unauthorized=new Web3(other,Host);
            await Send(true,first,identity,"unauthorized","OASIS storage owner required",unauthorized);
            Assert.AreEqual("ciphertext-two",await Read());
        }
        finally {await provider.DeActivateProviderAsync();}
    }
}
