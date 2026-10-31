using System.IO;
using System.Linq;
using FluentAssertions;
using Newtonsoft.Json.Linq;
using Xunit;

namespace NextGenSoftware.OASIS.API.DNA.UnitTests
{
    /// <summary>
    /// The sandbox and native import settings have one set of default values, in the DNA classes. The shipped default DNA file lists
    /// the same settings so they can be found and edited, and must never drift from those defaults.
    /// </summary>
    public class SandboxAndNativeImportDnaTests
    {
        private static JObject DefaultDnaOasisSection()
        {
            var directory = new DirectoryInfo(Directory.GetCurrentDirectory());
            while (directory != null && !File.Exists(Path.Combine(directory.FullName, "NextGenSoftware.OASIS.API.DNA", "Default", "OASIS_DNA.json")))
                directory = directory.Parent;
            directory.Should().NotBeNull("the default DNA file must be reachable from the test output");
            var path = Path.Combine(directory!.FullName, "NextGenSoftware.OASIS.API.DNA", "Default", "OASIS_DNA.json");
            return (JObject)JObject.Parse(File.ReadAllText(path))["OASIS"]!;
        }

        [Fact]
        public void TheDefaultDnaFileListsEveryNativeImportSettingAtItsDefault() =>
            AssertMatches(DefaultDnaOasisSection()["NativeImport"], new NativeImportSettings());

        [Fact]
        public void TheDefaultDnaFileListsEverySandboxSettingAtItsDefaultAndTheSandboxIsOff() =>
            AssertMatches(DefaultDnaOasisSection()["Sandbox"], new SandboxSettings());

        [Fact]
        public void ADnaWithoutTheSectionsStillGetsTheDefaults()
        {
            var dna = Newtonsoft.Json.JsonConvert.DeserializeObject<OASISDNA>("{\"OASIS\":{}}");
            dna!.OASIS.Sandbox.Enabled.Should().BeFalse();
            dna.OASIS.Sandbox.DemoAvatarRetentionMinutes.Should().Be(1440);
            dna.OASIS.NativeImport.AllowPrivateAddresses.Should().BeFalse();
            dna.OASIS.NativeImport.SandboxMaximumRecordsPerSource.Should().Be(500);
        }

        [Fact]
        public void APartialSectionOverridesOnlyWhatItNames()
        {
            var dna = Newtonsoft.Json.JsonConvert.DeserializeObject<OASISDNA>("{\"OASIS\":{\"Sandbox\":{\"Enabled\":true,\"DemoAvatarRetentionMinutes\":60}}}");
            dna!.OASIS.Sandbox.Enabled.Should().BeTrue();
            dna.OASIS.Sandbox.DemoAvatarRetentionMinutes.Should().Be(60);
            dna.OASIS.Sandbox.CleanupIntervalMinutes.Should().Be(15);
            dna.OASIS.Sandbox.DemoAvatarUsernamePrefix.Should().Be("sandbox-");
        }

        private static void AssertMatches(JToken? section, object defaults)
        {
            section.Should().NotBeNull("the default DNA file must list this section");
            var properties = defaults.GetType().GetProperties();
            foreach (var property in properties)
            {
                var listed = ((JObject)section!)[property.Name];
                listed.Should().NotBeNull($"{property.Name} must be listed in the default DNA file");
                listed!.ToObject(property.PropertyType).Should().Be(property.GetValue(defaults), $"{property.Name} in the default DNA file must equal its default");
            }
            ((JObject)section!).Properties().Select(p => p.Name).Should().BeEquivalentTo(properties.Select(p => p.Name),
                "the default DNA file must not list a setting the code does not have");
        }
    }
}
