using System;
using System.Linq;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Objects.NativeData;
using NextGenSoftware.OASIS.API.DNA;
using Xunit;

namespace NextGenSoftware.OASIS.API.Core.UnitTests
{
    /// <summary>Every sandbox and native import limit comes from DNA, and a bad value says which setting is wrong.</summary>
    [Collection("NativeConnectionGuard")]
    public class SandboxSettingsFromDnaTests : IDisposable
    {
        private readonly OASISDNA _original = OASISDNAManager.OASISDNA;

        public SandboxSettingsFromDnaTests() => OASISDNAManager.OASISDNA = new OASISDNA();

        public void Dispose() => OASISDNAManager.OASISDNA = _original;

        [Fact]
        public void DefaultsAreWrittenOnceInTheDnaClassesAndTheSandboxIsOffByDefault()
        {
            Assert.False(SandboxOptions.Enabled);
            Assert.Equal(TimeSpan.FromHours(24), SandboxOptions.Retention);
            Assert.Equal(new[] { ProviderType.MongoDBOASIS, ProviderType.SQLLiteDBOASIS, ProviderType.Neo4jOASIS }, SandboxOptions.Providers());
            Assert.False(NativeConnectionGuard.AllowPrivate);
            Assert.Equal(1000, NativeDataImportManager.MaximumPageSize);
        }

        [Fact]
        public void ChangingTheDnaChangesTheBehaviourWithoutACodeChange()
        {
            var dna = OASISDNAManager.OASISDNA.OASIS;
            dna.Sandbox.Enabled = true;
            dna.Sandbox.DemoAvatarRetentionMinutes = 90;
            dna.Sandbox.Providers = "MongoDBOASIS";
            dna.NativeImport.AllowPrivateAddresses = true;
            dna.NativeImport.MaximumPageSize = 50;
            dna.NativeImport.ConnectTimeoutSeconds = 3;

            Assert.True(SandboxOptions.Enabled);
            Assert.True(ProviderFaultGate.IsEnabled);
            Assert.Equal(TimeSpan.FromMinutes(90), SandboxOptions.Retention);
            Assert.Equal(new[] { ProviderType.MongoDBOASIS }, SandboxOptions.Providers());
            Assert.True(NativeConnectionGuard.AllowPrivate);
            Assert.Equal(50, NativeDataImportManager.MaximumPageSize);
            Assert.Equal(TimeSpan.FromSeconds(3), NativeImportOptions.ConnectTimeout);
        }

        [Fact]
        public void ABadValueNamesTheSettingInsteadOfRunningWithASurprise()
        {
            var dna = OASISDNAManager.OASISDNA.OASIS;
            dna.Sandbox.DemoAvatarRetentionMinutes = 0;
            Assert.Contains("DemoAvatarRetentionMinutes", Assert.Throws<InvalidOperationException>(() => SandboxOptions.Retention).Message);

            dna.Sandbox.Providers = "MongoDBOASIS, NotAProvider";
            Assert.Contains("NotAProvider", Assert.Throws<InvalidOperationException>(() => SandboxOptions.Providers()).Message);
            dna.Sandbox.Providers = " , ";
            Assert.Throws<InvalidOperationException>(() => SandboxOptions.Providers());

            dna.NativeImport.ReadTimeoutSeconds = -1;
            Assert.Contains("ReadTimeoutSeconds", Assert.Throws<InvalidOperationException>(() => NativeImportOptions.ReadTimeout).Message);

            dna.Sandbox.MaximumSessionsPerDay = 0;
            Assert.Throws<InvalidOperationException>(() => SandboxOptions.CreateSessionLimiter());
        }

        [Fact]
        public void TheSessionLimiterFollowsTheConfiguredLimits()
        {
            OASISDNAManager.OASISDNA.OASIS.Sandbox.MaximumSessionsPerAddressPerHour = 1;
            var limiter = SandboxOptions.CreateSessionLimiter();
            Assert.True(limiter.TryAcquire("1.2.3.4", out _));
            Assert.False(limiter.TryAcquire("1.2.3.4", out _));
        }
    }
}
