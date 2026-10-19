using VDS.RDF;
using VDS.RDF.Parsing;
using System.Reflection;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;

namespace NextGenSoftware.OASIS.API.Providers.SOLIDOASIS.UnitTests;

public class SolidGenesisTests
{
    public class BodyProxy : DispatchProxy
    {
        public Guid Id=Guid.Parse("7dc76317-8351-4a40-853f-eb8a4ae59ab9");
        protected override object? Invoke(MethodInfo? method,object?[]? args)=>method!.Name switch
        {
            "get_Id"=>Id,
            "get_Name"=>"App \"quoted\" \\ path\nsecond line",
            "get_Description"=>"Generated RDF description",
            "get_CelestialBodyCore"=>null,
            _=>throw new InvalidOperationException("Unexpected generation input access: "+method.Name)
        };
    }

    [Fact]
    public void CelestialBodyGenerationPreservesEscapedLabelsAsRealRdf()
    {
        var root=Path.Combine(Path.GetTempPath(),"oasis-solid-genesis-tests");
        if(Directory.Exists(root))throw new IOException("Retained genesis fixture exists; inspect it before reuse.");
        var output=Path.Combine(root,"SOLID","app.ttl");
        try
        {
            using var provider=new SOLIDOASIS("https://pod.invalid/private/");
            Assert.True(provider.NativeCodeGenesis(DispatchProxy.Create<ICelestialBody,BodyProxy>(),root,null!));
            using var graph=new Graph();new TurtleParser().Load(graph,output);
            Assert.Contains(graph.Triples,triple=>triple.Predicate is IUriNode predicate&&predicate.Uri.AbsoluteUri=="http://www.w3.org/2000/01/rdf-schema#label"
                &&triple.Object is ILiteralNode value&&value.Value=="App \"quoted\" \\ path\nsecond line");
        }
        finally {if(File.Exists(output))File.Delete(output);if(Directory.Exists(Path.GetDirectoryName(output)))Directory.Delete(Path.GetDirectoryName(output)!);if(Directory.Exists(root))Directory.Delete(root);}
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("not valid Turtle !!!")]
    [InlineData("@prefix oasis: <https://oasis.invalid/> .")]
    public void InvalidOrMissingSourceDoesNotOverwriteExistingArtifact(string? source)
    {
        var root=Path.Combine(Path.GetTempPath(),"oasis-solid-genesis-tests");
        if(Directory.Exists(root))throw new IOException("Retained genesis fixture exists; inspect it before reuse.");
        var output=Path.Combine(root,"SOLID","app.ttl");
        Directory.CreateDirectory(Path.GetDirectoryName(output)!);
        const string original="<https://oasis.invalid/app> <https://oasis.invalid/name> \"retained\" .";
        File.WriteAllText(output,original);
        try
        {
            using var provider=new SOLIDOASIS("https://pod.invalid/private/");
            Assert.False(provider.NativeCodeGenesis(null!,root,source!));
            Assert.Equal(original,File.ReadAllText(output));
        }
        finally {File.Delete(output);Directory.Delete(Path.GetDirectoryName(output)!);Directory.Delete(root);}
    }

    [Fact]
    public void ValidNativeSourceProducesParseableRdfWithoutPodIo()
    {
        var root=Path.Combine(Path.GetTempPath(),"oasis-solid-genesis-tests");
        if(Directory.Exists(root))throw new IOException("Retained genesis fixture exists; inspect it before reuse.");
        var output=Path.Combine(root,"SOLID","app.ttl");
        const string source="<https://oasis.invalid/app> <https://oasis.invalid/name> \"native app\" .";
        try
        {
            using var provider=new SOLIDOASIS("https://pod.invalid/private/");
            Assert.True(provider.NativeCodeGenesis(null!,root,source));
            using var graph=new Graph();new TurtleParser().Load(graph,output);
            Assert.Single(graph.Triples);Assert.Equal(source,File.ReadAllText(output));
        }
        finally {if(File.Exists(output))File.Delete(output);if(Directory.Exists(Path.GetDirectoryName(output)))Directory.Delete(Path.GetDirectoryName(output)!);if(Directory.Exists(root))Directory.Delete(root);}
    }
}
