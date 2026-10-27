using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Queries;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.IntegrationTests;

public sealed partial class CqlProjectedAllAcceptanceTests
{
    public sealed class SelectedElementDto
    {
        public static int Constructions;
        public SelectedElementDto() => Constructions++;
        public string Label { get; set; } = "";
    }

    private static Task VerifySelectedElements(ICosmicQueryStorageProvider adapter,
        IQueryable<Holon> root, IQueryable<Holon> multi, List<object> checks)
    {
        const string rootName = "protected source root";
        var names = root.Select(row => row.Name);
        var ordered = multi.OrderBy(row => CosmicQueryMetadata.Value<int>(row, "checkedOrder"));
        var orderedNames = ordered.Select(row => row.Name);

        Assert.AreEqual(rootName, names.First());
        Passed("selected scalar First");
        Assert.IsTrue(root.Select(row => row.Version).Single() > 0);
        Passed("selected value-type Single");

        SelectedElementDto.Constructions = 0;
        Assert.AreEqual(rootName, root.Select(row => new SelectedElementDto { Label = row.Name }).Single().Label);
        Assert.AreEqual(1, SelectedElementDto.Constructions);
        Passed("selected Single constructs exactly one DTO");

        // Cardinality and detached proofs must be checked before user constructors.
        SelectedElementDto.Constructions = 0;
        Assert.AreEqual("CQL_SEQUENCE_MULTIPLE", Assert.ThrowsException<CosmicLinqException>(() =>
            multi.Select(row => new SelectedElementDto { Label = row.Name }).Single()).Code);
        Assert.AreEqual(0, SelectedElementDto.Constructions);
        Passed("ambiguous selected Single constructs no DTO");
        Assert.AreEqual("CQL_SEQUENCE_MULTIPLE", Assert.ThrowsException<CosmicLinqException>(() =>
            multi.Select(row => row.Name).SingleOrDefault("fallback")).Code);
        Passed("ambiguous selected SingleOrDefault preserves error");

        Assert.AreEqual("visible match", orderedNames.ElementAt(1));
        Passed("selected forward ElementAt");
        Assert.AreEqual("owned cycle", orderedNames.ElementAt(^1));
        Passed("selected from-end ElementAt");
        Assert.AreEqual("visible match", ordered.Take(2).Select(row => row.Name).Last());
        Passed("selected Last respects source window");

        var selectedWindow = multi.Select(row => row.Name).OrderBy(value => value).Take(2);
        Assert.AreEqual(rootName, selectedWindow.Last());
        Passed("selected Last respects selected ordering and window");
        Assert.AreEqual(rootName, selectedWindow.First(value => value == rootName));
        Passed("selected First predicate remains after selected window");

        Assert.AreEqual("fallback", names.Where(value => value == "absent").FirstOrDefault("fallback"));
        Passed("selected empty caller-supplied default");
        Assert.IsNull(root.Select(row => CosmicQueryMetadata.Value<string>(row, "nullable")).FirstOrDefault("fallback"));
        Passed("present selected null is not an empty default");
        Assert.AreEqual("CQL_SEQUENCE_EMPTY", Assert.ThrowsException<CosmicLinqException>(() =>
            names.Where(value => value == "absent").First()).Code);
        Passed("required selected First empty error");
        Assert.IsNull(orderedNames.Take(0).LastOrDefault());
        Passed("selected LastOrDefault empty window");
        Assert.IsNull(names.ElementAtOrDefault(-1));
        Passed("negative selected ElementAtOrDefault");
        Assert.IsNull(orderedNames.ElementAtOrDefault(^0));
        Passed("selected from-end zero default");
        Assert.AreEqual("CQL_INDEX_OUT_OF_RANGE", Assert.ThrowsException<CosmicLinqException>(() =>
            orderedNames.ElementAt(9)).Code);
        Passed("selected ElementAt beyond native sequence");

        Assert.AreEqual("visible match", orderedNames.Single(value => value == "visible match"));
        Passed("selected Single predicate");
        Assert.AreEqual(rootName, orderedNames.Last(value => value == rootName));
        Passed("selected Last predicate");

        var requestedName = rootName;
        var rebound = orderedNames.Where(value => value == requestedName);
        Assert.AreEqual(rootName, rebound.Single());
        requestedName = "visible match";
        Assert.AreEqual(requestedName, rebound.Single());
        Passed("selected element predicate binds per execution");

        Assert.AreEqual(-9, root.Select(row => row.Version).Take(0).FirstOrDefault(-9));
        Passed("selected value-type caller-supplied empty default");
        Assert.AreEqual("visible match", orderedNames.ElementAt(^2));
        Passed("selected nontrivial from-end offset");
        Assert.AreEqual("CQL_ORDER_REQUIRED", Assert.ThrowsException<CosmicLinqException>(() => names.Last()).Code);
        Passed("selected tail requires explicit ordering");
        Assert.AreEqual("CQL_SEQUENCE_EMPTY", Assert.ThrowsException<CosmicLinqException>(() =>
            root.Select(row => row.Version).Take(0).Single()).Code);
        Passed("required selected Single empty error");

        return Task.CompletedTask;
        void Passed(string label) => checks.Add(new { provider = adapter.QueryProviderType.ToString(), label });
    }
}
