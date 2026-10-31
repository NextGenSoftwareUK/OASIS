using ProviderTestSupport;
using Xunit;

namespace NextGenSoftware.OASIS.API.Providers.TRONOASIS.UnitTests;

public class NativeCodeGenesisTests
{
    [Theory]
    [InlineData("id")] [InlineData("name")] [InlineData("description")]
    [InlineData("member")] [InlineData("holon")] [InlineData("contract")]
    public void SchemaNameCollisionsAreRejectedBeforeWriting(string collision)
        =>NativeCodeGenesisContract.VerifyNameCollision((body,path)=>new TRONOASIS().NativeCodeGenesis(body,path,""),collision);
    [Fact]
    public void GeneratedSchemaWithAdditionalFieldsCompiles()
        =>NativeCodeGenesisContract.VerifySchema((body,path)=>new TRONOASIS().NativeCodeGenesis(body,path,""));
    [Theory]
    [InlineData(null,false)] [InlineData("",false)] [InlineData("  ",false)]
    [InlineData("// 🌳\npragma solidity ^0.8.0; contract SourceFixture {}",true)]
    public void ResultRequiresAnActualSourceArtifact(string? source,bool expected)
        =>NativeCodeGenesisContract.Verify((path,text)=>new TRONOASIS().NativeCodeGenesis(null!,path,text!),source,expected);
}
