using System.Reflection;
using NextGenSoftware.OASIS.Common;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.Persistence.Repositories;

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.UnitTests;

[TestClass]
public sealed class CqlGroupedParticipationTests
{
    private static OASISResult<object> Decode(CosmicScalarOperation operation, Type type,
        long inputs, long high, long low, long present)
        => (OASISResult<object>)typeof(HolonRepository).GetMethod("DecodeGroupedNumeric",
            BindingFlags.NonPublic | BindingFlags.Static)!.Invoke(null, new object[] { operation, type, inputs, high, low, present })!;

    [DataTestMethod]
    [DataRow(0, false)] [DataRow(0, true)] [DataRow(1, false)] [DataRow(1, true)]
    [DataRow(2, false)] [DataRow(2, true)] [DataRow(3, false)] [DataRow(3, true)]
    [DataRow(4, false)] [DataRow(4, true)]
    public void NumericPacketMustAgreeWithTheContainingGroup(int corruption, bool average)
    {
        var operation = average ? CosmicScalarOperation.Average : CosmicScalarOperation.Sum;
        var type = corruption < 2 ? typeof(long) : typeof(long?);
        var groupCount = corruption switch { 0 => 1L, 1 => 2L, 2 => 1L, 3 => 0L, _ => -1L };
        var present = corruption switch { 1 => 1L, 2 => 2L, _ => 0L };
        var result = Decode(operation, type, groupCount, 0, present, present);
        Assert.IsTrue(result.IsError);
        Assert.AreEqual("CQL_DATA_INVALID", result.ErrorCode);
        Assert.IsNull(result.Result);
    }

    [DataTestMethod]
    [DataRow(0)] [DataRow(1)] [DataRow(2)] [DataRow(3)] [DataRow(4)] [DataRow(5)]
    public void RequiredAndNullableCoverageRetainTheirExistingNumericResults(int domain)
    {
        var type = domain < 2 ? typeof(int) : domain < 4 ? typeof(long) : typeof(long?);
        var present = domain == 5 ? 0L : domain == 4 ? 1L : 2L;
        var total = present == 2 ? 3L : present;
        var sum = Decode(CosmicScalarOperation.Sum, type, 2, 0, total, present);
        var mean = Decode(CosmicScalarOperation.Average, type, 2, 0, total, present);
        Assert.IsFalse(sum.IsError, sum.Message); Assert.IsFalse(mean.IsError, mean.Message);
        Assert.AreEqual(total, Convert.ToInt64(sum.Result));
        if (present == 0) Assert.IsNull(mean.Result);
        else Assert.AreEqual(present == 2 ? 1.5m : 1m, mean.Result);
    }
}
