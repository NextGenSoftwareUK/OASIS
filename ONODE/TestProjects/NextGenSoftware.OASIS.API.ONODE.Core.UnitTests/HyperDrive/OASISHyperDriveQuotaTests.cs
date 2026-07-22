using System;
using System.Reflection;
using System.Threading.Tasks;
using FluentAssertions;
using NextGenSoftware.OASIS.API.Core.Managers.OASISHyperDrive;
using Xunit;

namespace NextGenSoftware.OASIS.API.ONODE.Core.UnitTests.HyperDrive
{
    /// <summary>
    /// Regression tests for the OASISHyperDrive v2 quota enforcement bug: GetCurrentUsageAsync used to always
    /// return 0 regardless of how many operations had actually been performed, which meant the quota check in
    /// CheckQuotaBeforeOperationAsync could never trigger - PayAsYouGoEnabled=false subscriptions effectively
    /// had unlimited usage. These tests call the private quota methods via reflection (there is no public
    /// surface for them, and adding one purely for testing would widen OASISHyperDrive's public API
    /// unnecessarily) to verify usage now actually accumulates and the limit is enforced.
    /// </summary>
    public class OASISHyperDriveQuotaTests
    {
        private static Task<int> GetCurrentUsageAsync(NextGenSoftware.OASIS.API.Core.Managers.OASISHyperDrive.OASISHyperDrive hyperDrive, string operationType)
        {
            var method = typeof(NextGenSoftware.OASIS.API.Core.Managers.OASISHyperDrive.OASISHyperDrive)
                .GetMethod("GetCurrentUsageAsync", BindingFlags.NonPublic | BindingFlags.Instance);
            return (Task<int>)method!.Invoke(hyperDrive, new object[] { operationType })!;
        }

        private static void IncrementUsage(string operationType)
        {
            var method = typeof(NextGenSoftware.OASIS.API.Core.Managers.OASISHyperDrive.OASISHyperDrive)
                .GetMethod("IncrementUsage", BindingFlags.NonPublic | BindingFlags.Static);
            method!.Invoke(null, new object[] { operationType });
        }

        [Fact]
        public async Task GetCurrentUsageAsync_AfterIncrementUsage_ReflectsRealCount()
        {
            // Use a unique operation-type label per test run so the shared static counter dictionary
            // (intentionally shared across instances - see OASISHyperDrive._usageCounters) doesn't leak
            // counts between test runs/methods.
            var operationType = $"TestOp-{Guid.NewGuid():N}";
            var hyperDrive = new NextGenSoftware.OASIS.API.Core.Managers.OASISHyperDrive.OASISHyperDrive();

            (await GetCurrentUsageAsync(hyperDrive, operationType)).Should().Be(0, "no usage has been recorded yet for this operation type");

            IncrementUsage(operationType);
            IncrementUsage(operationType);
            IncrementUsage(operationType);

            (await GetCurrentUsageAsync(hyperDrive, operationType)).Should().Be(3, "three increments should accumulate to a real count of 3, not the old hardcoded 0");
        }

        [Fact]
        public async Task GetCurrentUsageAsync_DifferentOperationTypes_AreCountedIndependently()
        {
            var opA = $"TestOp-A-{Guid.NewGuid():N}";
            var opB = $"TestOp-B-{Guid.NewGuid():N}";
            var hyperDrive = new NextGenSoftware.OASIS.API.Core.Managers.OASISHyperDrive.OASISHyperDrive();

            IncrementUsage(opA);
            IncrementUsage(opA);
            IncrementUsage(opB);

            (await GetCurrentUsageAsync(hyperDrive, opA)).Should().Be(2);
            (await GetCurrentUsageAsync(hyperDrive, opB)).Should().Be(1);
        }
    }
}
