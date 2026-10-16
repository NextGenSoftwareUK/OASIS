using System;
using System.IO;
using NextGenSoftware.Holochain.HoloNET.ORM.Generation;
using Xunit;

namespace NextGenSoftware.OASIS.API.Providers.HoloOASIS.UnitTests
{
    /// <summary>
    /// STAR's hApp generation through HoloOASIS: a CelestialBody DNA folder in, a Holochain 0.7 hApp (and its HoloNET ORM
    /// models) out, from the Holochain07 templates. (The generated hApp is also compiled with cargo and run on real
    /// 0.7.0 / 0.7.1 conductors: HoloNET's scripts/build-generated-happ.sh + LiveGeneratedHappTests.)
    /// </summary>
    public class NativeCodeGenesisTests
    {
        // The templates as they ship in STAR (copied to STAR's output as DNATemplates/...).
        private static readonly string Templates = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory,
            "..", "..", "..", "..", "..", "..", "..", "STAR ODK", "NextGenSoftware.OASIS.STAR", "DNATemplates", "RustDNATemplates", "Holochain07"));

        private const string GameDna = @"
namespace Test
{
    public class Game : ZomeDNA
    {
        public class Player : HolonDNA
        {
            public string Name { get; set; }
            public int Level { get; set; }
            public long Score { get; set; }
            public bool IsOnline { get; set; }
            public double Rating { get; set; }
            public DateTime JoinedOn { get; set; }
            public Guid Id { get; set; }
        }

        public class GameItem : HolonDNA
        {
            public string Title { get; set; }
            public string Type { get; set; }
        }
    }
}";

        private static string NewDnaFolder(string root)
        {
            string dna = Path.Combine(root, "dna");
            Directory.CreateDirectory(dna);
            File.WriteAllText(Path.Combine(dna, "Game.cs"), GameDna);
            return dna;
        }

        [Theory]
        [InlineData("0.7.1", "=0.7.1", "=0.8.1")]
        [InlineData("0.7.0", "=0.7.0", "=0.8.0")]
        public void NativeCodeGenesis_FromDnaFolder_WritesAHolochain07HappFromTheTemplates(string version, string hdk, string hdi)
        {
            Assert.True(Directory.Exists(Templates), "templates not found at " + Templates);
            string root = Path.Combine(Path.GetTempPath(), "holooasis-genesis-" + Guid.NewGuid().ToString("N").Substring(0, 8));
            try
            {
                string nativeParams = System.Text.Json.JsonSerializer.Serialize(new System.Collections.Generic.Dictionary<string, string>
                {
                    ["celestialBodyDNAFolder"] = NewDnaFolder(root),
                    ["holochainVersion"] = version,
                    ["templateFolder"] = Templates
                });

                Assert.True(new HoloOASIS("ws://localhost:7777").NativeCodeGenesis(null, Path.Combine(root, "out"), nativeParams));

                string rust = Path.Combine(root, "out", "Rust");
                string cargo = File.ReadAllText(Path.Combine(rust, "Cargo.toml"));
                Assert.Contains($"hdk = \"{hdk}\"", cargo);
                Assert.Contains($"hdi = \"{hdi}\"", cargo);
                Assert.Contains("[workspace.dependencies.game_integrity]", cargo);
                Assert.DoesNotContain("{{", cargo);

                string integrity = File.ReadAllText(Path.Combine(rust, "dnas", "oapp", "zomes", "integrity", "game_integrity", "src", "lib.rs"));
                Assert.Contains("pub struct Player {", integrity);
                Assert.Contains("pub level: i32,", integrity);
                Assert.Contains("pub score: i64,", integrity);
                Assert.Contains("pub rating: f64,", integrity);
                Assert.Contains("pub joined_on: String,", integrity);
                Assert.Contains("pub r#type: String,", integrity);
                Assert.Contains("    Player(Player),", integrity);
                Assert.Contains("    AllPlayers,", integrity);
                Assert.DoesNotContain("{{", integrity);

                string coordinator = File.ReadAllText(Path.Combine(rust, "dnas", "oapp", "zomes", "coordinator", "game", "src", "lib.rs"));
                foreach (string fn in new[] { "create_player", "get_player", "update_player", "delete_player", "get_all_players", "get_player_collection", "add_player_to_collection", "remove_player_from_collection", "batch_update_player_collection", "create_game_item" })
                    Assert.Contains($"pub fn {fn}(", coordinator);
                Assert.DoesNotContain("{{", coordinator);

                Assert.Contains("name: game_integrity", File.ReadAllText(Path.Combine(rust, "dnas", "oapp", "workdir", "dna.yaml")));
                Assert.Contains("../dnas/oapp/workdir/oapp.dna", File.ReadAllText(Path.Combine(rust, "workdir", "happ.yaml")));

                string model = File.ReadAllText(Path.Combine(rust, "CSharp", "Player.cs"));
                Assert.Contains("public class Player : HoloNETEntryBase", model);
                Assert.Contains("[HolochainRustFieldName(\"is_online\")]", model);
                Assert.Contains("public bool IsOnline { get; set; }", model);
                Assert.Contains("[HolochainRustFieldName(\"type\")]", File.ReadAllText(Path.Combine(rust, "CSharp", "GameItem.cs")));
                Assert.DoesNotContain("{{", model);
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

        [Fact]
        public void TemplateEngine_MissingTemplatesOrEmptySpec_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => HolochainHappTemplateEngine.Generate(null, Templates, Path.GetTempPath()));
            Assert.Throws<DirectoryNotFoundException>(() => HolochainHappTemplateEngine.Generate(new HappSpec(), Path.Combine(Path.GetTempPath(), "no-templates-" + Guid.NewGuid()), Path.GetTempPath()));
            Assert.Throws<ArgumentException>(() => HolochainHappTemplateEngine.Generate(new HappSpec { Name = "x" }, Templates, Path.GetTempPath()));
        }

        /// <summary>
        /// Writes STAR's sample game hApp (HoloNET namespace HoloNETGame) where HoloNET's scripts/build-generated-happ.sh asks
        /// (HOLONET_GENERATE_HAPP_TO, HOLONET_GENERATE_HAPP_VERSION), so it can be compiled and run on real conductors.
        /// </summary>
        [Fact]
        public void WriteSampleHappForHoloNETBuildScript()
        {
            string to = Environment.GetEnvironmentVariable("HOLONET_GENERATE_HAPP_TO");
            if (string.IsNullOrEmpty(to))
                return; // only does something when run by the build script

            string root = Path.Combine(Path.GetTempPath(), "holooasis-sample-" + Guid.NewGuid().ToString("N").Substring(0, 8));
            try
            {
                HappSpec spec = HolochainHappGenerator.FromStarDnaFolder(NewDnaFolder(root), "holonet_game", "HoloNETGame");
                if (Environment.GetEnvironmentVariable("HOLONET_GENERATE_HAPP_VERSION") == "0.7.0")
                    spec.HolochainVersion = HappHolochainVersion.V0_7_0;
                HolochainHappTemplateEngine.Generate(spec, Templates, to);
            }
            finally
            {
                try { Directory.Delete(root, true); } catch { }
            }
        }
    }
}
