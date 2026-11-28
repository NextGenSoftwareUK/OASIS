using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Models.Data;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Cql;
namespace NextGenSoftware.OASIS.API.Core.UnitTests;
public sealed class CqlRecursiveSetV1CompilerTests
{
    private static CqlSetV1Source Leaf()=>new(){Take=2,Columns=new[]{new CqlProjectionV1Column{Field="Name",Alias="label"}}};
    private static CqlSetV1Request Tree()=>new(){Provider="MongoDBOASIS",Kind="Concat",Left=new(){Set=new(){Kind="Concat",Left=Leaf(),Right=Leaf(),Take=1}},Right=Leaf()};
    [Fact] public void CaptureDetachesNestedSourcesAndPreservesWindows()
    {
        var request=Tree();var copy=CqlRequestCapture.Capture(request);
        request.Left.Set!.Left.Columns![0].Alias="mutated";request.Left.Set.Take=0;
        Assert.Equal("label",copy.Left.Set!.Left.Columns![0].Alias);Assert.Equal(1,copy.Left.Set.Take);
        var actor=Guid.NewGuid();var query=CqlV1Compiler.CompileProjectedSet(copy,actor,ProviderType.MongoDBOASIS);
        Assert.NotNull(query.LeftSet);Assert.Equal(actor,query.Output.Source.ReadAccess.ActorId);
        Assert.Equal(2,query.LeftSet!.Left.Source.Limit);
    }
    [Fact] public void NestedEnvelopeCannotSmuggleLeafSettingsOrMixSequenceKinds()
    {
        var request=Tree();request.Left.Take=1;
        Assert.Throws<ArgumentException>(()=>CqlV1Compiler.CompileProjectedSet(request,Guid.NewGuid(),ProviderType.MongoDBOASIS));
        request=Tree();request.Left.Set!.Left.Columns=null;
        Assert.Throws<ArgumentException>(()=>CqlV1Compiler.CompileProjectedSet(request,Guid.NewGuid(),ProviderType.MongoDBOASIS));
    }
    [Fact] public void RecursiveDepthIsRejectedBeforeCompilation()
    {
        var request=Tree();for(var i=0;i<10;i++)request.Left=new(){Set=new(){Kind="Concat",Left=request.Left,Right=Leaf()}};
        Assert.Throws<ArgumentException>(()=>CqlRequestCapture.Capture(request));
        Assert.Throws<ArgumentException>(()=>CqlV1Compiler.CompileProjectedSet(request,Guid.NewGuid(),ProviderType.MongoDBOASIS));
    }
    [Fact] public void NumericReductionCapturesAndRequiresItsVisibleAlias()
    {
        var request=Tree();
        foreach(var leaf in new[]{request.Left.Set!.Left,request.Left.Set.Right,request.Right})
            leaf.Columns=new[]{new CqlProjectionV1Column{Field="Version",Alias="Amount"}};
        request.Reduction="Sum";request.ReductionColumn="Amount";request.MaximumResults=1;
        var copy=CqlRequestCapture.Capture(request);request.ReductionColumn="changed";
        Assert.Equal("Amount",copy.ReductionColumn);
        var query=CqlV1Compiler.CompileProjectedSetReduction(copy,Guid.NewGuid(),ProviderType.MongoDBOASIS);
        Assert.Equal("Amount",Assert.Single(query.Output.OutputAliases));
        copy.ReductionColumn="hidden";
        Assert.Throws<ArgumentException>(()=>CqlV1Compiler.CompileProjectedSetReduction(copy,Guid.NewGuid(),ProviderType.MongoDBOASIS));
        copy.ReductionColumn=null;
        Assert.Throws<ArgumentException>(()=>CqlV1Compiler.CompileProjectedSetReduction(copy,Guid.NewGuid(),ProviderType.MongoDBOASIS));
    }
    [Fact] public void ExtremaRequireTheirVisibleAliasWithoutNumericCoercion()
    {
        var request=Tree();request.Reduction="Min";request.ReductionColumn="label";
        var query=CqlV1Compiler.CompileProjectedSetReduction(request,Guid.NewGuid(),ProviderType.MongoDBOASIS);
        Assert.Equal(typeof(string),query.Output.Columns[query.Output.OutputColumnIndices[0]].ValueType);
        request.Reduction="Max";request.ReductionColumn="hidden";
        Assert.Throws<ArgumentException>(()=>CqlV1Compiler.CompileProjectedSetReduction(request,Guid.NewGuid(),ProviderType.MongoDBOASIS));
        request.ReductionColumn=null;
        Assert.Throws<ArgumentException>(()=>CqlV1Compiler.CompileProjectedSetReduction(request,Guid.NewGuid(),ProviderType.MongoDBOASIS));
    }
    [Fact] public void EntityInputStagesCaptureAfterSourceWindowAndRejectNestedSmuggling()
    {
        var request=Tree();var leaf=request.Left.Set!.Left;
        leaf.EntityStages=new[]{new CqlSetV1EntityStage{Operation="Take",Count=1}};
        var copy=CqlRequestCapture.Capture(request);leaf.EntityStages[0].Count=0;
        Assert.Equal(1,copy.Left.Set!.Left.EntityStages![0].Count);
        var actor=Guid.NewGuid();var query=CqlV1Compiler.CompileProjectedSet(copy,actor,ProviderType.MongoDBOASIS);
        Assert.True(query.HasEntityInputs);var entity=query.LeftSet!.LeftInput.EntityProjection;
        Assert.NotNull(entity);Assert.Equal(2,entity!.Source.Source.Limit);
        Assert.Single(entity.Source.Stages);Assert.Equal(actor,entity.Selection.Source.ReadAccess.ActorId);
        copy.Left.EntityStages=new[]{new CqlSetV1EntityStage{Operation="Take",Count=0}};
        Assert.Throws<ArgumentException>(()=>CqlV1Compiler.CompileProjectedSet(copy,actor,ProviderType.MongoDBOASIS));
        copy.Left.EntityStages=null;copy.Left.Set.Left.Columns=null;
        Assert.Throws<ArgumentException>(()=>CqlV1Compiler.CompileProjectedSet(copy,actor,ProviderType.MongoDBOASIS));
    }

}
