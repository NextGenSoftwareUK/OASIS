using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Interfaces;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.IntegrationTests;

public sealed partial class CqlV1Web4NativeTests
{
    public interface IPropertyOutcomeBoundary : IRecoveryMutationBoundary, IConditionalHolonPropertyStorageProvider { }

    // Observe the actual native reader; never synthesize an outcome or intercept its I/O.
    public class PropertyOutcomeBoundary : SaveDemoBoundary
    {
        public int OutcomeReads, PreparationReads;
        protected override object? Invoke(MethodInfo? method, object?[]? args)
        {
            if (method!.Name == nameof(IConditionalHolonPropertyStorageProvider.ReadConditionalHolonPropertyWriteOutcomeAsync))
                OutcomeReads++;
            if (method.Name == nameof(IHolonWritePreparationRecoveryStorageProvider.ReadHolonWritePreparationAsync))
                PreparationReads++;
            return base.Invoke(method, args);
        }
    }

    [DataTestMethod]
    [DataRow("MongoDBOASIS")] [DataRow("SQLLiteDBOASIS")] [DataRow("Neo4jOASIS")]
    public Task PropertySelectionStatusUsesPinnedManagerAndActualNativeReader(string providerName)
        => RunPropertyNativeFixture(providerName, true, true, true);
}
