using System.Reflection;
using System.Runtime.ExceptionServices;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Serialization;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.IntegrationTests;

public sealed partial class CqlV1Web4NativeTests
{
    // Observe real preparation/transaction calls without changing the request,
    // synthesizing an acknowledgement or replaying a mutation.
    public class SaveDemoBoundary : DispatchProxy
    {
        public IOASISStorageProvider Target = null!;
        public IHolon Source = null!;
        public Func<Guid, IEnumerable<Guid>, Task> BeforeResource = null!;
        public readonly List<Guid> MutationIntents = new();
        public readonly List<Guid> PreparationIntents = new();
        public int SuccessfulMutations, GeneratedCommitObservations;

        protected override object? Invoke(MethodInfo? method, object?[]? args)
        {
            if (method!.Name == nameof(IHolonWritePreparationStorageProvider.ClaimHolonWritePreparationAsync))
                return Claim((HolonWritePreparation)args![0]!, (CancellationToken)args[1]!);
            if (method.Name == nameof(IConditionalHolonStorageProvider.ExecuteConditionalHolonWriteAsync))
                return Mutate((ConditionalHolonWriteRequest)args![0]!, (CancellationToken)args[1]!);
            try { return method.Invoke(Target, args); }
            catch (TargetInvocationException cause) when (cause.InnerException != null)
            { ExceptionDispatchInfo.Capture(cause.InnerException).Throw(); throw; }
        }

        private async Task<OASISResult<HolonWritePreparation>> Claim(
            HolonWritePreparation request, CancellationToken cancellationToken)
        {
            // Journal generated IDs before even the protected intent is stored.
            PreparationIntents.Add(request.OperationId);
            await BeforeResource(request.OperationId, request.Records.Select(record => record.HolonId));
            return await ((IHolonWritePreparationStorageProvider)Target)
                .ClaimHolonWritePreparationAsync(request, cancellationToken);
        }

        private async Task<OASISResult<IEnumerable<IHolon>>> Mutate(
            ConditionalHolonWriteRequest request, CancellationToken cancellationToken)
        {
            MutationIntents.Add(request.Write.OperationId);
            await BeforeResource(request.Write.OperationId, request.Write.Conditions.Select(condition => condition.HolonId));
            var before = StoredRecordJson.Serialize(Source);
            var wasUnassigned = Source.Id == Guid.Empty;
            var result = await ((IConditionalHolonStorageProvider)Target)
                .ExecuteConditionalHolonWriteAsync(request, cancellationToken);
            if (!result.IsError)
            {
                SuccessfulMutations++;
                Assert.AreEqual(before, StoredRecordJson.Serialize(Source),
                    "After real native commit, caller publication must still await acknowledgement validation.");
                if (wasUnassigned)
                {
                    Assert.AreEqual(Guid.Empty, Source.Id);
                    Assert.IsTrue(result.Result.All(record => record.Id != Guid.Empty));
                    GeneratedCommitObservations++;
                }
            }
            return result;
        }
    }
}
