using System;
using System.Threading.Tasks;
using Moq;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;
using Xunit;

namespace NextGenSoftware.OASIS.API.Core.UnitTests
{
    /// <summary>
    /// Providers report absence with <see cref="OASISResultErrorCodes.RecordNotFound"/>. Legacy get-or-create callers
    /// (allowMissing, settings holons, native import) depend on HolonManager recognising that exact code.
    /// </summary>
    public class HolonManagerAllowMissingTests
    {
        private static (HolonManager Manager, Mock<IOASISStorageProvider> Storage) Legacy(OASISResult<IHolon> loadResult)
        {
            var dna = new OASISDNA { OASIS = new NextGenSoftware.OASIS.API.DNA.OASIS { HyperDriveMode = HyperDriveModes.Legacy } };
            dna.OASIS.StorageProviders ??= new StorageProviderSettings();
            dna.OASIS.StorageProviders.ProviderMethodCallTimeOutSeconds = 10;
            var providers = new ProviderManager(null, dna);
            var storage = new Mock<IOASISStorageProvider>();
            storage.SetupGet(value => value.ProviderType).Returns(new EnumValue<ProviderType>(ProviderType.MongoDBOASIS));
            storage.SetupGet(value => value.ProviderCategory).Returns(new EnumValue<ProviderCategory>(ProviderCategory.Storage));
            storage.SetupGet(value => value.IsProviderActivated).Returns(true);
            storage.Setup(value => value.ActivateProviderAsync()).ReturnsAsync(new OASISResult<bool>(true));
            storage.Setup(value => value.LoadHolonAsync(It.IsAny<Guid>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<int>(),
                It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<int>())).ReturnsAsync(loadResult);
            providers.RegisterProvider(storage.Object);
            return (new HolonManager(null, dna, providers), storage);
        }

        [Fact]
        public async Task ProviderRecordNotFoundIsAnAllowedAbsence()
        {
            var (manager, _) = Legacy(new OASISResult<IHolon> { ErrorCode = OASISResultErrorCodes.RecordNotFound, Message = "Holon not found." });

            var result = await manager.LoadHolonAsync(Guid.NewGuid(), loadChildren: false, recursive: false,
                providerType: ProviderType.MongoDBOASIS, allowMissing: true);

            Assert.False(result.IsError, result.Message);
            Assert.Null(result.Result);
            Assert.Equal(OASISResultErrorCodes.RecordNotFound, result.ErrorCode);
        }

        [Fact]
        public async Task OutageIsNotTreatedAsAbsence()
        {
            var (manager, _) = Legacy(new OASISResult<IHolon> { IsError = true, Message = "connection refused" });

            var result = await manager.LoadHolonAsync(Guid.NewGuid(), loadChildren: false, recursive: false,
                providerType: ProviderType.MongoDBOASIS, allowMissing: true);

            Assert.True(result.IsError || result.IsWarning);
            Assert.NotEqual(OASISResultErrorCodes.RecordNotFound, result.ErrorCode);
        }
    }
}
