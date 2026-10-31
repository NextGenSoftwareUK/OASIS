using Microsoft.VisualStudio.TestTools.UnitTesting;
using Nethereum.Web3;
using Nethereum.Web3.Accounts;
using Nethereum.RPC.Eth.DTOs;
using Nethereum.Hex.HexTypes;
using Nethereum.Hex.HexConvertors.Extensions;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.Enums;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.EthereumOASIS.IntegrationTests;

public partial class EthereumOASISIntegrationTests
{
    [DataTestMethod][DoNotParallelize]
    [DataRow("Ethereum")][DataRow("Arbitrum")][DataRow("Polygon")][DataRow("Shared")]
    public async Task NativeStatusUsesCanonicalFinalityAndRealRevertReceipt(string kind)
    {
        Assert.AreEqual("http://127.0.0.1:18545",Host,"Only retained local development runtime is authorized.");
        Assert.AreEqual(new System.Numerics.BigInteger(31337),ChainId);
        var (ethereum,address)=await CreateActivatedProviderAsync();
        var account=new Account(PrivateKey,ChainId);var web3=new Web3(account,Host);
        Func<string,Task<OASISResult<BridgeTransactionStatus>>> observe=hash=>ethereum.GetTransactionStatusAsync(hash);
        Func<Task> close=async()=>{};
        if(kind=="Arbitrum"){
            var provider=new NextGenSoftware.OASIS.API.Providers.ArbitrumOASIS.ArbitrumOASIS(Host,PrivateKey,ChainId,address);
            Assert.IsFalse((await provider.ActivateProviderAsync()).IsError);observe=hash=>provider.GetTransactionStatusAsync(hash);close=async()=>{await provider.DeActivateProviderAsync();};
        }else if(kind=="Polygon"){
            var provider=new NextGenSoftware.OASIS.API.Providers.PolygonOASIS.PolygonOASIS(Host,PrivateKey,address,ChainId);
            Assert.IsFalse((await provider.ActivateProviderAsync()).IsError);observe=hash=>provider.GetTransactionStatusAsync(hash);close=async()=>{await provider.DeActivateProviderAsync();};
        }else if(kind=="Shared"){
            var provider=new SharedMintFixture(Host,PrivateKey,address);
            Assert.IsFalse((await provider.ActivateProviderAsync()).IsError);observe=hash=>provider.GetTransactionStatusAsync(hash);close=async()=>{await provider.DeActivateProviderAsync();};
        }
        var fixture=Environment.GetEnvironmentVariable("ETHEREUMOASIS_TEST_FIXTURE_JSON")??throw new InvalidOperationException("Use the retained native runner.");
        var work=Path.GetDirectoryName(Path.GetFullPath(fixture))!;Assert.IsTrue(Directory.Exists(work));
        var journal=Path.Combine(work,"native-status-"+kind+".pending.json");
        try{
            Assert.IsFalse(File.Exists(journal),"Reconcile original local status fixture before resubmission.");
            var absent=await observe("0x"+new string('f',64));Assert.IsFalse(absent.IsError,absent.Exception?.ToString());Assert.AreEqual(BridgeTransactionStatus.Pending,absent.Result);
            var success=await web3.Eth.GetEtherTransferService().TransferEtherAndWaitForReceiptAsync("0x2222222222222222222222222222222222222222",0.000000000000000001m);
            Assert.AreEqual(System.Numerics.BigInteger.One,success.Status.Value);
            var observed=await observe(success.TransactionHash);Assert.IsFalse(observed.IsError,observed.Exception?.ToString());Assert.AreEqual(BridgeTransactionStatus.Completed,observed.Result);
            var input=new TransactionInput{From=account.Address,To=address,Data="0xdeadbeef",Value=new HexBigInteger(0),Gas=new HexBigInteger(100000),
                GasPrice=await web3.Eth.GasPrice.SendRequestAsync(),Nonce=await web3.Eth.Transactions.GetTransactionCount.SendRequestAsync(account.Address)};
            var raw=await web3.TransactionManager.SignTransactionAsync(input);
            var expectedHash=new Nethereum.Util.Sha3Keccack().CalculateHash(raw.HexToByteArray()).ToHex(true);
            using(var file=new FileStream(journal,FileMode.CreateNew,FileAccess.Write,FileShare.None,4096,FileOptions.WriteThrough)){
                var bytes=System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(new{kind,transactionHash=expectedHash,nonce=input.Nonce.Value.ToString(),chainId=ChainId.ToString(),state="before-local-revert-broadcast"});
                await file.WriteAsync(bytes);file.Flush(true);
            }
            try{Assert.AreEqual(expectedHash,await web3.Eth.Transactions.SendRawTransaction.SendRequestAsync(raw));}
            catch(Nethereum.JsonRpc.Client.RpcResponseException){/* Hardhat reports a mined revert as an RPC error. Require the independently read exact receipt below; never rebroadcast. */}
            var failed=await web3.Eth.Transactions.GetTransactionReceipt.SendRequestAsync(expectedHash);
            Assert.IsNotNull(failed,"Unknown original outcome must retain journal and fail.");Assert.AreEqual(expectedHash,failed.TransactionHash);Assert.AreEqual(System.Numerics.BigInteger.Zero,failed.Status.Value);
            var finalized=await web3.Eth.Blocks.GetBlockWithTransactionsHashesByNumber.SendRequestAsync(BlockParameter.CreateFinalized());
            Assert.IsTrue(failed.BlockNumber.Value<=finalized.Number.Value);
            var canonical=await web3.Eth.Blocks.GetBlockWithTransactionsHashesByNumber.SendRequestAsync(new BlockParameter(failed.BlockNumber));Assert.AreEqual(failed.BlockHash,canonical.BlockHash);
            var outcome=await observe(expectedHash);Assert.IsFalse(outcome.IsError,outcome.Exception?.ToString());Assert.AreEqual(BridgeTransactionStatus.Failed,outcome.Result);
            File.Delete(journal);
        }finally{await close();await ethereum.DeActivateProviderAsync();}
    }
}
