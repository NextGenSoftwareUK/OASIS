using Microsoft.VisualStudio.TestTools.UnitTesting;
using ProviderTestSupport;

namespace NextGenSoftware.OASIS.API.Providers.HashgraphOASIS.UnitTests;

[TestClass,DoNotParallelize]
public class NativeCodeGenesisTests
{
    [DataTestMethod]
    [DataRow("id")] [DataRow("name")] [DataRow("description")]
    [DataRow("member")] [DataRow("holon")] [DataRow("contract")]
    public void SchemaNameCollisionsAreRejectedBeforeWriting(string collision)
        =>NativeCodeGenesisContract.VerifyNameCollision((body,path)=>new HashgraphOASIS().NativeCodeGenesis(body,path,""),collision);
    [TestMethod]
    public void GeneratedSchemaWithAdditionalFieldsCompiles()
        =>NativeCodeGenesisContract.VerifySchema((body,path)=>new HashgraphOASIS(network:"testnet").NativeCodeGenesis(body,path,""));
    [DataTestMethod]
    [DataRow(null,false)] [DataRow("",false)] [DataRow("  ",false)]
    [DataRow("// 🌳\npragma solidity ^0.8.0; contract SourceFixture {}",true)]
    public void ResultRequiresAnActualSourceArtifact(string? source,bool expected)
        =>NativeCodeGenesisContract.Verify((path,text)=>new HashgraphOASIS(network:"testnet").NativeCodeGenesis(null!,path,text!),source,expected);
}
