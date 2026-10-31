using System.Text.Json;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Models.Data;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Cql;

namespace NextGenSoftware.OASIS.API.Core.UnitTests;

public sealed class CqlJoinV1CompilerTests
{
    private static CqlJoinV1Request Request()=>new()
    {
        Kind="Left",
        Left=new(){Skip=2,Take=3},Right=new(){Skip=4,Take=5,Descending=true},
        Keys=new[]{new CqlJoinV1Key{Alias="product",Left=new(){Field="Id"},Right=new(){MetadataPath=new[]{"productId"},ValueType="Guid"}}},
        Columns=new[]{new CqlJoinV1Column{Alias="name",Source="Left",Selection=new(){Field="Name"}},
            new CqlJoinV1Column{Alias="version",Source="Right",Selection=new(){Field="Version"},AllowMissing=true},
            new CqlJoinV1Column{Alias="hasRight",Source="RightPresence"}},
        MaximumLeftRows=17,MaximumRightRows=19,MaximumPairs=100,MaximumResults=25
    };

    [Theory]
    [InlineData(ProviderType.MongoDBOASIS)] [InlineData(ProviderType.SQLLiteDBOASIS)] [InlineData(ProviderType.Neo4jOASIS)]
    public void SourcesBindTheHostActorProviderWindowsAndIndependentWorkLimits(ProviderType provider)
    {
        var actor=Guid.NewGuid();var captured=CqlV1Compiler.CompileJoin(Request(),actor,provider);
        Assert.Equal(actor,captured.Join.Left.ReadAccess.ActorId);Assert.Equal(actor,captured.Join.Right.ReadAccess.ActorId);
        Assert.Equal(new[]{provider},captured.Join.Left.Scope.Providers);Assert.Equal(new[]{provider},captured.Join.Right.Scope.Providers);
        Assert.Equal(2,captured.Join.Left.Offset);Assert.Equal(3,captured.Join.Left.Limit);
        Assert.Equal(4,captured.Join.Right.Offset);Assert.Equal(5,captured.Join.Right.Limit);
        Assert.Equal(17,captured.Join.Options.MaximumLeftRows);Assert.Equal(19,captured.Join.Options.MaximumRightRows);
        Assert.Equal(100,captured.Join.Options.MaximumPairs);Assert.Equal(25,captured.Join.Options.MaximumResults);
        Assert.Equal(1024*1024,captured.Join.Options.MaximumPayloadBytes);
        Assert.Equal(typeof(int?),captured.Columns[1].ValueType);Assert.Equal(typeof(int),captured.Columns[1].Path.ValueType);
    }

    [Theory]
    [InlineData(0)] [InlineData(1)] [InlineData(2)] [InlineData(3)] [InlineData(4)]
    [InlineData(5)] [InlineData(6)] [InlineData(7)] [InlineData(8)] [InlineData(9)]
    public void UnsupportedOrAmbiguousShapesRejectBeforeProviderResolution(int mutation)
    {
        var request=Request();
        switch(mutation)
        {
            case 0:request.Kind="Cross";break;
            case 1:request.Keys=Array.Empty<CqlJoinV1Key>();break;
            case 2:request.Keys[0].Right!.ValueType="Int64";break;
            case 3:request.Columns[1].AllowMissing=false;break;
            case 4:request.Columns[2].Selection=new(){Field="Name"};break;
            case 5:request.Columns[0].WhenMissing=true;break;
            case 6:request.MaximumResults=101;break;
            case 7:request.MaximumPairs=10001;break;
            case 8:request.MaximumLeftRows=1001;break;
            default:request.Columns[2].Alias="name";break;
        }
        Assert.Throws<ArgumentException>(()=>CqlV1Compiler.CompileJoin(request,Guid.NewGuid(),ProviderType.SQLLiteDBOASIS));
    }

    [Theory]
    [InlineData("{\"actor\":\"forged\"}")]
    [InlineData("{\"left\":{\"provider\":\"MongoDBOASIS\"}}")]
    [InlineData("{\"columns\":[{\"expression\":\"arbitrary code\"}]}")]
    public void JsonCannotInjectActorsPerSourceProvidersOrExecutableExpressions(string json)
        =>Assert.Throws<JsonException>(()=>JsonSerializer.Deserialize<CqlJoinV1Request>(json,new JsonSerializerOptions{PropertyNameCaseInsensitive=true}));

    [Fact]
    public void CompiledMetadataPathsAndPresenceOptionsDoNotRetainMutableRequests()
    {
        var request=Request();request.Columns[2].WhenMissing=true;
        var captured=CqlV1Compiler.CompileJoin(request,Guid.NewGuid(),ProviderType.MongoDBOASIS);
        request.Keys[0].Right!.MetadataPath![0]="changed";request.Columns[0].Alias="changed";
        Assert.Equal(new[]{"productId"},captured.Join.Keys[0].Right.Segments);
        Assert.Equal("name",captured.Columns[0].Alias);Assert.True(captured.Columns[2].PresenceWhenMissing);
    }

    [Fact]
    public void EmptyHostActorNeverProducesAJoinIntent()
        =>Assert.Throws<ArgumentException>(()=>CqlV1Compiler.CompileJoin(Request(),Guid.Empty,ProviderType.MongoDBOASIS));
}
