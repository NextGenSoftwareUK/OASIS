using Microsoft.VisualStudio.TestTools.UnitTesting;
using MongoDB.Bson;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;

namespace NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.UnitTests;

[TestClass]
public sealed class MongoCqlSourceRelationshipTests
{
    [DataTestMethod,DataRow(false),DataRow(true)]
    public void BooleanSourceRelationshipsKeepProtectedErrorsAheadOfEmptyPaging(bool conjunction)
    {
        var owner=Guid.NewGuid();var bounds=CosmicQueryTraversalOptions.Create(2,1).Result;
        var source=CosmicQuery<Holon>.Create().Where(row=>row.Id==owner).Result;
        var query=conjunction
            ?source.Where(row=>row.Name=="never" && CosmicQueryRelations.AnyDescendant(row,child=>true,bounds))
            :source.Where(row=>row.Name=="always" || CosmicQueryRelations.AnyDescendant(row,child=>true,bounds));
        Assert.IsFalse(query.IsError,query.Message);
        var command=new MongoCqlTranslator().CompileExecution(query.Take(0).Result,10);
        Assert.IsTrue(command.RequiresMembership);Assert.IsTrue(command.HasTraversalEnvelope);
        var first=command.Pipeline[0].ToJson();StringAssert.Contains(first,owner.ToString("D"));
        Assert.IsFalse(first.Contains("never") || first.Contains("always"),"A compound business predicate cannot pre-filter its protected relationship scope.");
        var lookup=Array.FindLastIndex(command.Pipeline,stage=>stage.Contains("$lookup"));
        var protectedOutcome=Array.FindIndex(command.Pipeline,stage=>stage.Contains("$set") && stage["$set"].AsBsonDocument.Contains("allMatched"));
        Assert.IsTrue(lookup>=0 && protectedOutcome>lookup);
        var facet=command.Pipeline.Single(stage=>stage.Contains("$facet"))["$facet"].AsBsonDocument;
        Assert.IsTrue(facet.Contains("invalid"));Assert.IsTrue(facet.Contains("roots"));
        StringAssert.Contains(facet["roots"].ToJson(),"\"$literal\" : false");
    }

    [TestMethod]
    public void ChildDescendantSourceUsesTheSharedProtectedEvaluator()
    {
        var bounds=CosmicQueryTraversalOptions.Create(2,1).Result;
        var query=CosmicQuery<Holon>.Create().WhereAnyChild(child=>child.Name=="first match"
            || CosmicQueryRelations.AnyDescendant(child,descendant=>true,bounds));
        Assert.IsFalse(query.IsError,query.Message);
        var command=new MongoCqlTranslator().CompileExecution(query.Result,10);
        Assert.IsTrue(command.RequiresMembership);Assert.IsTrue(command.HasTraversalEnvelope);
        var pipeline=command.Pipeline.ToJson();StringAssert.Contains(pipeline,"\"$lookup\"");
        StringAssert.Contains(pipeline,"\"error\"");StringAssert.Contains(pipeline,"\"$facet\"");
    }

    [TestMethod]
    public void NestedSourceVisibilityIsCapturedAtEveryRelationshipScope()
    {
        var actor=Guid.NewGuid();var bounds=CosmicQueryTraversalOptions.Create(2,2).Result;
        var query=CosmicQuery<Holon>.Create().WithReadAccess(CosmicQueryReadAccess.OwnerOrPublic(actor).Result)
            .WhereAnyChild(child=>CosmicQueryRelations.AnyDescendant(child,descendant=>descendant.Name=="leaf",bounds));
        Assert.IsFalse(query.IsError,query.Message);
        var command=new MongoCqlTranslator().CompileExecution(query.Result,10);
        var lookup=command.Pipeline.Single(stage=>stage.Contains("$lookup")).ToJson();
        StringAssert.Contains(lookup,actor.ToString("D"));StringAssert.Contains(lookup,"CreatedByAvatarId");StringAssert.Contains(lookup,"IsPublic");
    }
}
