using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.Common;
using Solnet.Rpc;
using Solnet.Rpc.Types;
using Solnet.Wallet;
using System.Text.Json;

namespace NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.IntegrationTests;

public sealed partial class SOLANAOASISIntegrationTests
{
    private sealed class AvatarTransferProvider(Account service, string recipient) : SolanaOASIS(
        Environment.GetEnvironmentVariable("SOLANA_RPC_URL")!,service.PrivateKey.Key,service.PublicKey.Key)
    {
        internal int SenderReads, RecipientReads;
        internal override Task<OASISResult<IProviderWallet>> LoadTransferWalletAsync(AvatarTransferSelector selector,string identity,bool signing)
        {
            if(signing) SenderReads++; else RecipientReads++;
            var address=signing ? Environment.GetEnvironmentVariable("SOLANA_PUBLIC_KEY")! : recipient;
            return Task.FromResult(new OASISResult<IProviderWallet> { IsLoaded=true, Result=new ProviderWallet {
                ProviderType=Core.Enums.ProviderType.SolanaOASIS, PublicKey=address, WalletAddress=address,
                PrivateKey=signing ? Environment.GetEnvironmentVariable("SOLANA_PRIVATE_KEY")! : null
            } });
        }
    }

    [DataTestMethod, TestCategory("AvatarTransferNative")]
    [DataRow(false)] [DataRow(true)]
    public async Task AvatarNativeTransfersUseIndependentSenderAndExactFinalizedBalances(bool sync)
    {
        var work=Environment.GetEnvironmentVariable("OASIS_SOLANA_NATIVE_EVIDENCE")!;
        var pending=Path.Combine(work,"avatar-transfer.pending.json");
        var complete=Path.Combine(work,sync ? "avatar-transfer-sync.complete.json" : "avatar-transfer-async.complete.json");
        Assert.IsFalse(File.Exists(pending));Assert.IsFalse(File.Exists(pending+".next"));Assert.IsFalse(File.Exists(complete));Assert.IsFalse(File.Exists(complete+".next"));
        var rpc=ClientFactory.GetClient(Environment.GetEnvironmentVariable("SOLANA_RPC_URL")!);
        var sender=Environment.GetEnvironmentVariable("SOLANA_PUBLIC_KEY")!;
        var recipient=new Account().PublicKey.Key; var service=new Account();
        Assert.AreNotEqual(sender,service.PublicKey.Key);
        var provider=new AvatarTransferProvider(service,recipient);
        var before=await rpc.GetBalanceAsync(sender,Commitment.Finalized);Assert.IsTrue(before.WasSuccessful);
        var recipientBefore=await rpc.GetBalanceAsync(recipient,Commitment.Finalized);Assert.IsTrue(recipientBefore.WasSuccessful);Assert.AreEqual(0UL,recipientBefore.Result.Value);
        var signatures=new List<string>(); var phase="before-first-transfer"; ulong fees=0; ulong? senderAfter=null, recipientAfter=null;
        async Task Journal(string path,bool create)
        {
            var bytes=JsonSerializer.SerializeToUtf8Bytes(new { phase,sender,recipient,service=service.PublicKey.Key,sync,signatures,feeLamports=fees,transferredLamports=4_000_000UL,senderBefore=before.Result.Value,senderAfter,recipientAfter,localTestOnly=true });
            var next=path+".next";
            await using(var file=new FileStream(next,FileMode.CreateNew,FileAccess.Write,FileShare.None))
            { await file.WriteAsync(bytes); file.Flush(true); }
            File.Move(next,path,overwrite:!create);
        }
        await Journal(pending,true);
        var from=Guid.NewGuid();var to=Guid.NewGuid();
        for(var selector=0;selector<4;selector++)
        {
            phase="before-selector-"+selector;await Journal(pending,false);
            var sent=selector switch {
                0 => sync ? provider.SendTransactionById(from,to,.001m) : await provider.SendTransactionByIdAsync(from,to,.001m),
                1 => sync ? provider.SendTransactionByUsername("synthetic-sender","synthetic-recipient",.001m) : await provider.SendTransactionByUsernameAsync("synthetic-sender","synthetic-recipient",.001m),
                2 => sync ? provider.SendTransactionByEmail("sender@example.invalid","recipient@example.invalid",.001m) : await provider.SendTransactionByEmailAsync("sender@example.invalid","recipient@example.invalid",.001m),
                _ => sync ? provider.SendTransactionByDefaultWallet(from,to,.001m) : await provider.SendTransactionByDefaultWalletAsync(from,to,.001m)
            };
            if(sent.IsError) {
                phase="unresolved-selector-"+selector;
                if(sent.Exception is NextGenSoftware.OASIS.API.Core.Interfaces.INativeMutationReconciliationRequired unknown)
                    signatures.Add(unknown.OriginalTransactionId);
                await Journal(pending,false);
            }
            Assert.IsFalse(sent.IsError,sent.Exception?.ToString());Assert.IsTrue(sent.IsSaved);Assert.IsNotNull(sent.Result);
            signatures.Add(sent.Result.TransactionResult);phase="acknowledged-selector-"+selector;await Journal(pending,false);
        }
        Assert.AreEqual(4,signatures.Distinct().Count());
        foreach(var signature in signatures)
        {
            var transaction=await rpc.GetTransactionAsync(signature,Commitment.Finalized);
            Assert.IsTrue(transaction.WasSuccessful);Assert.IsNotNull(transaction.Result);Assert.IsNull(transaction.Result.Meta.Error);
            Assert.AreEqual(sender,transaction.Result.Transaction.Message.AccountKeys[0]);
            fees=checked(fees+Convert.ToUInt64(transaction.Result.Meta.Fee));
        }
        var after=await rpc.GetBalanceAsync(sender,Commitment.Finalized);var received=await rpc.GetBalanceAsync(recipient,Commitment.Finalized);
        Assert.IsTrue(after.WasSuccessful);Assert.IsTrue(received.WasSuccessful);
        Assert.AreEqual(4_000_000UL,received.Result.Value);Assert.AreEqual(checked(4_000_000UL+fees),checked(before.Result.Value-after.Result.Value));
        senderAfter=after.Result.Value;recipientAfter=received.Result.Value;
        Assert.AreEqual(4,provider.SenderReads);Assert.AreEqual(4,provider.RecipientReads);
        phase="verified";await Journal(complete,true);
        // Native local-test recipient/balance is retained in this one reusable ledger and recorded, not silently reset.
        File.Delete(pending);
    }
}
