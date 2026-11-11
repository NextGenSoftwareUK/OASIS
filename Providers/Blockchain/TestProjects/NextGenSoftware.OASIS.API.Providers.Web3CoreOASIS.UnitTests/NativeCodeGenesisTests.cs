using Microsoft.VisualStudio.TestTools.UnitTesting;
using ProviderTestSupport;

namespace NextGenSoftware.OASIS.API.Providers.Web3CoreOASIS.UnitTests;

[TestClass, DoNotParallelize]
public sealed class NativeCodeGenesisTests
{
    private readonly Web3CoreOASISBaseProvider _provider = new("", "", "");

    [DataTestMethod]
    [DataRow(null, false)]
    [DataRow("", false)]
    [DataRow("  ", false)]
    [DataRow("pragma solidity ^0.8.0; contract SourceFixture {}", true)]
    public void ResultRequiresAnActualSourceArtifact(string? source, bool expected)
        => NativeCodeGenesisContract.Verify((path, text) => _provider.NativeCodeGenesis(null!, path, text!), source, expected);

    [DataTestMethod]
    [DataRow("id")]
    [DataRow("member")]
    [DataRow("holon")]
    [DataRow("contract")]
    public void SchemaNameCollisionsAreRejectedBeforeWriting(string collision)
        => NativeCodeGenesisContract.VerifyNameCollision((body, path) => _provider.NativeCodeGenesis(body, path, ""), collision);

    [TestMethod]
    public void GeneratedSchemaWithAdditionalFieldsCompiles()
    {
        if (Environment.GetEnvironmentVariable("OASIS_WEB3CORE_SOLIDITY_COMPILER_TEST") != "1")
            Assert.Inconclusive("Enable this check with the repository's pinned Solidity compiler installed.");

        NativeCodeGenesisContract.VerifySchema((body, path) => _provider.NativeCodeGenesis(body, path, ""));
    }
}
