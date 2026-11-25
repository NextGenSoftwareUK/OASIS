using System;
using NextGenSoftware.OASIS.API.Core.Enums;
using Xunit;

namespace NextGenSoftware.OASIS.API.Providers.ScuttlebuttOASIS.UnitTests
{
    /// <summary>Provider identity and constructor validation; no storage service is needed.</summary>
    public class ScuttlebuttOASISTests
    {
        private static ScuttlebuttOASIS Create() => new("http://127.0.0.1:18904", "token", new byte[32]);

        [Fact]
        public void Constructor_ShouldInitializeProviderIdentity()
        {
            var provider = Create();
            Assert.Equal("ScuttlebuttOASIS", provider.ProviderName);
            Assert.False(string.IsNullOrWhiteSpace(provider.ProviderDescription));
            Assert.Equal(ProviderType.ScuttlebuttOASIS, provider.ProviderType.Value);
            Assert.Equal(ProviderCategory.StorageAndNetwork, provider.ProviderCategory.Value);
            Assert.False(provider.IsProviderActivated);
        }

        [Fact]
        public void Constructor_RequiresA32ByteEncryptionKey()
            => Assert.Throws<ArgumentException>(() => new ScuttlebuttOASIS("http://127.0.0.1:18904", "token", new byte[16]));

        [Fact]
        public void Constructor_RequiresTheServiceToken()
            => Assert.Throws<ArgumentException>(() => new ScuttlebuttOASIS("http://127.0.0.1:18904", "", new byte[32]));

        [Fact]
        public void Constructor_RequiresAnHttpServiceUrl()
            => Assert.Throws<ArgumentException>(() => new ScuttlebuttOASIS("ftp://127.0.0.1:18904", "token", new byte[32]));

        [Fact]
        public async System.Threading.Tasks.Task Activation_FailsExplicitlyWhenTheServiceIsUnreachable()
        {
            var result = await new ScuttlebuttOASIS("http://127.0.0.1:1", "token", new byte[32]).ActivateProviderAsync();
            Assert.True(result.IsError);
            Assert.False(result.Result);
        }
    }
}
