// Example: SmartContractManager Integration
// This file demonstrates how to use the SmartContractManager in OASIS

using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Examples
{
    public class SmartContractExample
    {
        // Example 1: Generate and compile a Solana contract from CelestialBody
        public async Task<OASISResult<string>> GenerateAndCompileSolanaContractAsync(
            ICelestialBody celestialBody)
        {
            var result = new OASISResult<string>();

            try
            {
                // Step 1: Generate Rust code using SolanaOASIS
                var solanaProvider = ProviderManager.GetStorageProvider<SolanaOASIS>(ProviderType.SolanaOASIS);
                string outputFolder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
                bool generated = solanaProvider.NativeCodeGenesis(celestialBody, outputFolder);

                if (!generated)
                {
                    result.Message = "Failed to generate Rust code";
                    result.IsError = true;
                    return result;
                }

                // Step 2: Create Anchor project ZIP
                string rustPath = Path.Combine(outputFolder, "Solana", "lib.rs");
                byte[] projectZip = CreateAnchorProjectZip(rustPath, outputFolder);

                // Step 3: Compile using SmartContractManager
                var compileResult = await SmartContractManager.CompileContractAsync(
                    SmartContractLanguage.Rust,
                    projectZip);

                if (compileResult.IsError)
                {
                    result.Message = compileResult.Message;
                    result.IsError = true;
                    return result;
                }

                // Step 4: Save compiled artifacts
                string compiledPath = Path.Combine(outputFolder, "compiled.so");
                await File.WriteAllBytesAsync(compiledPath, compileResult.Result.CompiledBytecode);

                result.Result = compiledPath;
                result.IsError = false;
                result.Message = "Contract compiled successfully";
            }
            catch (Exception ex)
            {
                result.Message = ex.Message;
                result.IsError = true;
            }

            return result;
        }

        // Example 2: Generate, compile, and deploy a token contract
        public async Task<OASISResult<DeployContractResult>> DeployTokenContractAsync(
            string tokenName,
            string symbol,
            decimal totalSupply)
        {
            var result = new OASISResult<DeployContractResult>();

            try
            {
                // Step 1: Create JSON specification
                var spec = new
                {
                    contract_type = "token",
                    blockchain = "solana",
                    language = "Rust",
                    framework = "Anchor",
                    spec = new
                    {
                        name = tokenName,
                        symbol = symbol,
                        total_supply = totalSupply,
                        decimals = 9,
                        features = new[] { "mint", "burn", "transfer" }
                    }
                };

                string jsonSpec = System.Text.Json.JsonSerializer.Serialize(spec);

                // Step 2: Generate contract
                var generateResult = await SmartContractManager.GenerateContractAsync(
                    SmartContractLanguage.Rust,
                    jsonSpec);

                if (generateResult.IsError)
                {
                    result.Message = generateResult.Message;
                    result.IsError = true;
                    return result;
                }

                // Step 3: Create project ZIP from generated code
                byte[] projectZip = CreateProjectZipFromGeneratedCode(generateResult.Result);

                // Step 4: Compile
                var compileResult = await SmartContractManager.CompileContractAsync(
                    SmartContractLanguage.Rust,
                    projectZip);

                if (compileResult.IsError)
                {
                    result.Message = compileResult.Message;
                    result.IsError = true;
                    return result;
                }

                // Step 5: Get wallet for deployment
                var walletResult = await WalletManager.GetWalletAsync(/* avatar ID */);
                if (walletResult.IsError)
                {
                    result.Message = walletResult.Message;
                    result.IsError = true;
                    return result;
                }

                // Step 6: Deploy
                var deployResult = await SmartContractManager.DeployContractAsync(
                    SmartContractLanguage.Rust,
                    compileResult.Result.CompiledBytecode,
                    compileResult.Result.AbiOrSchema,
                    walletResult.Result.PrivateKey);

                result.Result = deployResult.Result;
                result.IsError = false;
            }
            catch (Exception ex)
            {
                result.Message = ex.Message;
                result.IsError = true;
            }

            return result;
        }

        // Example 3: Compile existing Rust source code
        public async Task<OASISResult<byte[]>> CompileExistingRustCodeAsync(
            string rustSourcePath)
        {
            var result = new OASISResult<byte[]>();

            try
            {
                // Read Rust source
                string rustCode = await File.ReadAllTextAsync(rustSourcePath);

                // Create Anchor project structure
                byte[] projectZip = CreateAnchorProjectZip(rustSourcePath, Path.GetDirectoryName(rustSourcePath));

                // Compile
                var compileResult = await SmartContractManager.CompileContractAsync(
                    SmartContractLanguage.Rust,
                    projectZip);

                if (compileResult.IsError)
                {
                    result.Message = compileResult.Message;
                    result.IsError = true;
                    return result;
                }

                result.Result = compileResult.Result.CompiledBytecode;
                result.IsError = false;
            }
            catch (Exception ex)
            {
                result.Message = ex.Message;
                result.IsError = true;
            }

            return result;
        }

        // Helper: Create Anchor project ZIP
        private byte[] CreateAnchorProjectZip(string rustSourcePath, string basePath)
        {
            using var memoryStream = new MemoryStream();
            using (var archive = new System.IO.Compression.ZipArchive(memoryStream, System.IO.Compression.ZipArchiveMode.Create, true))
            {
                // Create Anchor.toml
                var anchorToml = archive.CreateEntry("Anchor.toml");
                using (var writer = new StreamWriter(anchorToml.Open()))
                {
                    writer.WriteLine("[features]");
                    writer.WriteLine("no-entrypoint = []");
                    writer.WriteLine("no-idl = []");
                    writer.WriteLine("no-log-ix-name = []");
                    writer.WriteLine("cpi = [\"no-entrypoint\"]");
                    writer.WriteLine("idl-build = [\"anchor-lang/idl-build\", \"anchor-spl/idl-build\"]");
                    writer.WriteLine();
                    writer.WriteLine("[programs.localnet]");
                    writer.WriteLine("your_program = \"YourProgramIdHere\"");
                }

                // Create Cargo.toml (workspace)
                var cargoToml = archive.CreateEntry("Cargo.toml");
                using (var writer = new StreamWriter(cargoToml.Open()))
                {
                    writer.WriteLine("[workspace]");
                    writer.WriteLine("members = [\"programs/*\"]");
                    writer.WriteLine("resolver = \"2\"");
                }

                // Create programs directory structure
                string programName = Path.GetFileNameWithoutExtension(rustSourcePath).ToSnakeCase();
                var programCargoToml = archive.CreateEntry($"programs/{programName}/Cargo.toml");
                using (var writer = new StreamWriter(programCargoToml.Open()))
                {
                    writer.WriteLine("[package]");
                    writer.WriteLine($"name = \"{programName}\"");
                    writer.WriteLine("version = \"1.0.0\"");
                    writer.WriteLine("edition = \"2021\"");
                    writer.WriteLine();
                    writer.WriteLine("[lib]");
                    writer.WriteLine("crate-type = [\"cdylib\", \"lib\"]");
                    writer.WriteLine();
                    writer.WriteLine("[dependencies]");
                    writer.WriteLine("anchor-lang = \"0.32.1\"");
                    writer.WriteLine("anchor-spl = \"0.32.1\"");
                }

                // Add lib.rs
                var libRs = archive.CreateEntry($"programs/{programName}/src/lib.rs");
                using (var sourceStream = File.OpenRead(rustSourcePath))
                using (var entryStream = libRs.Open())
                {
                    sourceStream.CopyTo(entryStream);
                }
            }

            return memoryStream.ToArray();
        }

        private byte[] CreateProjectZipFromGeneratedCode(byte[] generatedCode)
        {
            // Similar to CreateAnchorProjectZip but from generated code bytes
            throw new NotImplementedException();
        }
    }
}
