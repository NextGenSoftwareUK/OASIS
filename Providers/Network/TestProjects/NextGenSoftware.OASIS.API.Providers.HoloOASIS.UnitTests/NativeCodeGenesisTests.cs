using System;
using System.IO;
using Xunit;

namespace NextGenSoftware.OASIS.API.Providers.HoloOASIS.UnitTests
{
    /// <summary>
    /// STAR's hApp generation through HoloOASIS: a CelestialBody DNA folder in, a Holochain 0.7 hApp (and its HoloNET ORM
    /// models) out. (The generated hApp itself is compiled with cargo and run on real conductors by HoloNET's
    /// LiveGeneratedHappTests.)
    /// </summary>
    public class NativeCodeGenesisTests
    {
        [Theory]
        [InlineData(null, "=0.7.1")]
        [InlineData("0.7.0", "=0.7.0")]
        public void NativeCodeGenesis_FromDnaFolder_WritesAHolochain07Happ(string version, string hdk)
        {
            string root = Path.Combine(Path.GetTempPath(), "holooasis-genesis-" + Guid.NewGuid().ToString("N").Substring(0, 8));
            string dnaFolder = Path.Combine(root, "dna");
            Directory.CreateDirectory(dnaFolder);
            File.WriteAllText(Path.Combine(dnaFolder, "SuperZome.cs"), @"
namespace Test
{
    public class SuperZome : ZomeDNA
    {
        public class SuperTest : HolonDNA
        {
            public string TestString { get; set; }
            public int TestInt { get; set; }
            public bool TestBool { get; set; }
        }
    }
}");
            try
            {
                string nativeParams = System.Text.Json.JsonSerializer.Serialize(new System.Collections.Generic.Dictionary<string, string>
                {
                    ["celestialBodyDNAFolder"] = dnaFolder,
                    ["holochainVersion"] = version ?? "0.7.1"
                });

                Assert.True(new HoloOASIS("ws://localhost:7777").NativeCodeGenesis(null, Path.Combine(root, "out"), nativeParams));

                string rust = Path.Combine(root, "out", "Rust");
                Assert.Contains($"hdk = \"{hdk}\"", File.ReadAllText(Path.Combine(rust, "Cargo.toml")));
                string coordinator = File.ReadAllText(Path.Combine(rust, "dnas", "oapp", "zomes", "coordinator", "super_zome", "src", "lib.rs"));
                Assert.Contains("pub fn create_super_test(", coordinator);
                Assert.Contains("pub fn get_super_test_collection(", coordinator);
                string integrity = File.ReadAllText(Path.Combine(rust, "dnas", "oapp", "zomes", "integrity", "super_zome_integrity", "src", "lib.rs"));
                Assert.Contains("pub test_int: i32,", integrity);
                Assert.Contains("public class SuperTest : HoloNETEntryBase", File.ReadAllText(Path.Combine(rust, "CSharp", "SuperTest.cs")));
            }
            finally
            {
                try { Directory.Delete(root, true); } catch { }
            }
        }

        [Fact]
        public void NativeCodeGenesis_NothingToGenerate_ReturnsFalse()
        {
            Assert.False(new HoloOASIS("ws://localhost:7777").NativeCodeGenesis(null, "", null));
            Assert.False(new HoloOASIS("ws://localhost:7777").NativeCodeGenesis(null, Path.GetTempPath(), null));
        }
    }
}
