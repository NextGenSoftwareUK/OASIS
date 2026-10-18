using Solnet.Wallet;
using NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.Infrastructure.Services.Solana;
using SplAccount=Solnet.Programs.Models.TokenProgram.TokenAccount;
using SplMint=Solnet.Programs.Models.TokenProgram.TokenMint;

namespace NextGenSoftware.OASIS.API.Providers.SOLANAOASIS;

public partial class SolanaOASIS
{
    private static ulong SplRawAmount(decimal amount,int decimals)=>SolanaLegacyTokenClient.SplRawAmount(amount,decimals);
    private Task<SplMint> ReadLegacyMintAsync(PublicKey mint)=>new SolanaLegacyTokenClient(_rpcClient).ReadLegacyMintAsync(mint);
    private Task<int> ReadLegacyMintDecimalsAsync(PublicKey mint)=>new SolanaLegacyTokenClient(_rpcClient).ReadLegacyMintDecimalsAsync(mint);
    private Task<SplAccount> ReadLegacyTokenAccountAsync(PublicKey address,PublicKey mint,PublicKey owner,bool allowMissing)
        =>new SolanaLegacyTokenClient(_rpcClient).ReadLegacyTokenAccountAsync(address,mint,owner,allowMissing);
    private Task<string> BurnLegacySplAsync(Account signer,PublicKey mint,decimal amount)
        =>new SolanaLegacyTokenClient(_rpcClient).BurnLegacySplAsync(signer,mint,amount);
    private Task<string> SendLegacySplAsync(Account signer,PublicKey recipient,PublicKey mint,decimal amount,string memo)
        =>new SolanaLegacyTokenClient(_rpcClient).SendLegacySplAsync(signer,recipient,mint,amount,memo);
    private Task<string> SubmitTokenInstructionsAsync(Account signer,System.Collections.Generic.IEnumerable<Solnet.Rpc.Models.TransactionInstruction> instructions,string memo)
        =>new SolanaLegacyTokenClient(_rpcClient).SubmitTokenInstructionsAsync(signer,instructions,memo);
}
