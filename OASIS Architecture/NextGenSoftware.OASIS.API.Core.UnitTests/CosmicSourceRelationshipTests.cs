using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Queries;
using Xunit;

namespace NextGenSoftware.OASIS.API.Core.UnitTests;

public sealed class CosmicSourceRelationshipTests
{
    private static readonly CosmicQueryTraversalOptions Bounds=CosmicQueryTraversalOptions.Create(2,4).Result;
    private static CosmicQueryCapabilities Profile(bool composed)
        => new(CosmicQueryFeatures.And|CosmicQueryFeatures.Or|CosmicQueryFeatures.Not|CosmicQueryFeatures.BooleanConstant
            |CosmicQueryFeatures.ChildExists|CosmicQueryFeatures.DescendantExists
            |(composed?CosmicQueryFeatures.SourceRelationshipComposition:CosmicQueryFeatures.None),
            new[]{typeof(string),typeof(Guid),typeof(int),typeof(bool)},Enum.GetValues<CosmicQueryComparisonOperator>(),new CosmicQueryTraversalCapability(32,1000));

    [Fact]
    public void NestedSourceIntentNeedsIndependentPermissionButExistingLeafFormsDoNot()
    {
        var bounds=Bounds; var source=CosmicQuery<Holon>.Create();
        var query=source.Where(root=>root.Children.Any(child=>CosmicQueryRelations.AnyDescendant(child,descendant=>descendant.Name=="leaf",bounds)));
        Assert.False(query.IsError,query.Message);
        Assert.Equal("CQL_CAPABILITY_UNSUPPORTED",Profile(false).Admit(query.Result).ErrorCode);
        Assert.False(Profile(true).Admit(query.Result).IsError);
        Assert.False(Profile(false).Admit(source.WhereAnyChild(child=>child.Name=="leaf").Result).IsError);
        Assert.False(Profile(false).Admit(source.WhereAnyDescendant(child=>child.Name=="leaf",bounds).Result).IsError);
    }

    [Fact]
    public void RootBooleanRelationshipCompositionCannotBorrowLeafTraversalPermission()
    {
        var bounds=Bounds; var query=CosmicQuery<Holon>.Create().Where(root=>root.Name=="root" || CosmicQueryRelations.AnyDescendant(root,child=>true,bounds));
        Assert.False(query.IsError,query.Message);
        Assert.Equal("CQL_CAPABILITY_UNSUPPORTED",Profile(false).Admit(query.Result).ErrorCode);
        Assert.False(Profile(true).Admit(query.Result).IsError);
    }

    [Fact]
    public void NativeExpansionIsBoundedBeforeTheNewSourceShapeCanBeDispatched()
    {
        var large=CosmicQueryTraversalOptions.Create(32,1000).Result;
        var query=CosmicQuery<Holon>.Create().WhereAnyDescendant(child=>CosmicQueryRelations.AnyDescendant(child,descendant=>true,large),large);
        Assert.False(query.IsError,query.Message);
        Assert.Equal("CQL_QUERY_LIMIT",Profile(true).Admit(query.Result).ErrorCode);
    }

    private static bool Callback(IHolon child)=>throw new Exception("A source predicate callback must not run.");
    [Fact]
    public void SourceCaptureFreezesLiteralsAndKeepsScalarAndSelectedGrammarsIndependent()
    {
        var bounds=Bounds; var label="before";
        var query=CosmicQuery<Holon>.Create().WhereAnyChild(child=>CosmicQueryRelations.AnyDescendant(child,descendant=>descendant.Name==label,bounds));
        Assert.False(query.IsError,query.Message);label="after";
        var child=Assert.IsType<CosmicQueryAnyChild>(Assert.Single(query.Result.Predicates));
        var descendant=Assert.IsType<CosmicQueryAnyDescendant>(child.Predicate);
        Assert.Equal("before",Assert.IsType<CosmicQueryComparison>(descendant.Predicate).Literal.Value);
        Assert.Equal("CQL_EXPRESSION_UNSUPPORTED",CosmicQueryExpression.Compile<Holon>(root=>root.Children.Any(child=>true)).ErrorCode);
        Assert.Equal("CQL_EXPRESSION_UNSUPPORTED",CosmicQuery<Holon>.Create().Where(root=>root.Children.Any(child=>Callback(child))).ErrorCode);
    }
}
