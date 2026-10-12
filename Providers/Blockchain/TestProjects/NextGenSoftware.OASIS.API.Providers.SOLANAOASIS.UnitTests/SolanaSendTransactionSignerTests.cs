using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.Entities.DTOs.Common;
using NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.Entities.DTOs.Requests;
using NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.Infrastructure.Services.Solana;
using Solnet.Wallet;

namespace NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.UnitTests
{
    /// <summary>
    /// SendTransaction must be signed by the account it sends from. These cases are rejected before any RPC call,
    /// so no network (or RPC client) is needed.
    /// </summary>
    [TestClass]
    public class SolanaSendTransactionSignerTests
    {
        private static SendTransactionRequest Request(Account from) => new SendTransactionRequest
        {
            FromAccount = new BaseAccountRequest { PublicKey = from.PublicKey.Key },
            ToAccount = new BaseAccountRequest { PublicKey = new Account().PublicKey.Key },
            Amount = 1,
            Lampposts = 1,
            MemoText = "test"
        };

        [TestMethod]
        public async Task FromAccountOtherThanTheOasisAccount_WithoutSigner_IsRejectedClearly()
        {
            var service = new SolanaService(new Account(), null);
            var otherWallet = new Account();

            var result = await service.SendTransaction(Request(otherWallet));

            Assert.IsTrue(result.IsError);
            StringAssert.Contains(result.Message, "must match the signing account");
        }

        [TestMethod]
        public async Task SignerThatIsNotTheFromAccount_IsRejected()
        {
            var service = new SolanaService(new Account(), null);

            var result = await service.SendTransaction(Request(new Account()), signerAccount: new Account());

            Assert.IsTrue(result.IsError);
            StringAssert.Contains(result.Message, "must match the signing account");
        }
    }
}
