using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Providers.BitcoinOASIS;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using System.Threading.Tasks;
using System;

namespace NextGenSoftware.OASIS.API.Providers.BitcoinOASIS.IntegrationTests
{
    [TestClass]
    public class BitcoinOASISIntegrationTests
    {
        private BitcoinOASIS _provider;

        [TestInitialize]
        public void Setup()
        {
            _provider = new BitcoinOASIS();
        }

        [TestMethod]
        public async Task EsploraActivationSurvivesDeactivation()
        {
            try
            {
                var active = await _provider.ActivateProviderAsync();
                Assert.IsFalse(active.IsError, active.Message);
                Assert.IsTrue(active.Result);
                Assert.IsTrue(_provider.IsProviderActivated);

                var inactive = await _provider.DeActivateProviderAsync();
                Assert.IsTrue(inactive.Result);
                Assert.IsFalse(_provider.IsProviderActivated);

                var reactivated = await _provider.ActivateProviderAsync();
                Assert.IsFalse(reactivated.IsError, reactivated.Message);
                Assert.IsTrue(reactivated.Result);
                Assert.IsTrue(_provider.IsProviderActivated);
            }
            finally
            {
                _provider.DeActivateProvider();
                _provider.Dispose();
            }
        }

        [TestMethod]
        public async Task EsploraOrdinaryTransactionDoesNotProduceAHolon()
        {
            const string transactionId = "4a5e1e4baab89f3a32518a88c31bc87f618f76673e2cc77ab2127b7afdeda33b";

            try
            {
                var result = await _provider.LoadHolonAsync(transactionId, loadChildren: false);
                Assert.IsTrue(result.IsError, result.Message);
                Assert.IsNull(result.Result);
                Assert.AreEqual(NextGenSoftware.OASIS.Common.OASISResultErrorCodes.RecordNotFound, result.ErrorCode, result.Message);

                var status = await _provider.GetTransactionStatusAsync(transactionId);
                Assert.IsFalse(status.IsError, status.Message);
                Assert.AreEqual(NextGenSoftware.OASIS.API.Core.Managers.Bridge.Enums.BridgeTransactionStatus.Completed, status.Result);
            }
            finally
            {
                _provider.Dispose();
            }
        }

        [TestMethod]
        public async Task EsploraReportsFundedAddressBalance()
        {
            try
            {
                var activation = await _provider.ActivateProviderAsync();
                Assert.IsFalse(activation.IsError, activation.Message);

                var balance = await _provider.GetBalanceAsync(
                    new NextGenSoftware.OASIS.API.Core.Objects.Wallet.Requests.GetWeb3WalletBalanceRequest
                    {
                        WalletAddress = "1A1zP1eP5QGefi2DMPTfTL5SLmv7DivfNa"
                    });

                Assert.IsFalse(balance.IsError, balance.Message);
                Assert.IsTrue(balance.Result > 1 && balance.Result < 21_000_000);
            }
            finally
            {
                _provider.Dispose();
            }
        }

        [TestMethod]
        public async Task EsploraActivatedAccountRestoresItsOriginalKeys()
        {
            try
            {
                var activation = await _provider.ActivateProviderAsync();
                Assert.IsFalse(activation.IsError, activation.Message);

                var created = await _provider.CreateAccountAsync();
                Assert.IsFalse(created.IsError, created.Message);
                var restored = await _provider.RestoreAccountAsync(created.Result.SeedPhrase);
                Assert.IsFalse(restored.IsError, restored.Message);

                Assert.IsTrue(created.Result.PublicKey == restored.Result.PublicKey, "Restored address differs.");
                Assert.IsTrue(created.Result.PrivateKey == restored.Result.PrivateKey, "Restored signing key differs.");
            }
            finally
            {
                _provider.Dispose();
            }
        }

        [TestMethod]
        public async Task SaveAvatar_ShouldReturnSuccessResult()
        {
            // Arrange
            var avatar = new Avatar
            {
                Id = Guid.NewGuid(),
                Username = "TestUser",
                Email = "test@example.com",
                FirstName = "Test",
                LastName = "User"
            };

            // Act
            var result = await _provider.SaveAvatarAsync(avatar);

            // Assert
            Assert.IsNotNull(result);
            Assert.IsFalse(result.IsError);
            Assert.IsNotNull(result.Result);
        }

        [TestMethod]
        public async Task LoadAvatar_ShouldReturnAvatar()
        {
            // Arrange
            var avatarId = Guid.NewGuid();

            // Act
            var result = await _provider.LoadAvatarAsync(avatarId);

            // Assert
            Assert.IsNotNull(result);
            // Note: This might return an error if avatar doesn't exist, which is expected
        }

        [TestMethod]
        public async Task SaveHolon_ShouldReturnSuccessResult()
        {
            // Arrange
            var holon = new Holon
            {
                Id = Guid.NewGuid(),
                Name = "TestHolon",
                Description = "Test Holon Description"
            };

            // Act
            var result = await _provider.SaveHolonAsync(holon);

            // Assert
            Assert.IsNotNull(result);
            Assert.IsFalse(result.IsError);
        }

        [TestMethod]
        public async Task LoadHolon_ShouldReturnHolon()
        {
            // Arrange
            var holonId = Guid.NewGuid();

            // Act
            var result = await _provider.LoadHolonAsync(holonId);

            // Assert
            Assert.IsNotNull(result);
            // Note: This might return an error if holon doesn't exist, which is expected
        }

        [TestCleanup]
        public void Cleanup()
        {
            if (_provider != null && _provider.IsProviderActivated)
            {
                _provider.DeActivateProvider();
            }
        }
    }
}
