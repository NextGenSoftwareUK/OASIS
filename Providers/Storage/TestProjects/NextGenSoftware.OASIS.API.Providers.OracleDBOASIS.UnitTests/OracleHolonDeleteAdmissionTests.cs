using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace NextGenSoftware.OASIS.API.Providers.OracleDBOASIS.UnitTests;

[TestClass]
public sealed class OracleHolonDeleteAdmissionTests
{
    public static IEnumerable<object[]> Cases()
    {
        foreach (var alias in Enumerable.Range(0, 6))
        foreach (var sync in new[] { false, true })
        foreach (var invalid in new[] { false, true })
            yield return new object[] { alias, sync, invalid };
    }

    [DataTestMethod]
    [DynamicData(nameof(Cases), DynamicDataSourceType.Method)]
    public async Task EmptyIdRejectsBeforeNativeConfiguration(int alias, bool sync, bool invalid)
    {
        var provider = new OracleDBOASIS("UnsupportedOracleOptionForOwnedTest=true");
        var id = invalid ? Guid.Empty : Guid.NewGuid();
        void Check<T>(NextGenSoftware.OASIS.Common.OASISResult<T> result)
        {
            Assert.IsTrue(result.IsError);
            Assert.IsFalse(result.IsDeleted);
            Assert.AreEqual(0, result.DeletedCount);
            Assert.IsInstanceOfType<ArgumentException>(result.Exception);
            var cause = (ArgumentException)result.Exception;
            if (invalid) { Assert.AreEqual("id", cause.ParamName); Assert.AreEqual("HYPERDRIVE_PAYLOAD_INVALID", result.ErrorCode); }
            else Assert.AreNotEqual("id", cause.ParamName);
        }
        switch (alias)
        {
            case 0: Check(sync ? provider.DeleteHolon(id) : await provider.DeleteHolonAsync(id)); break;
            case 1: Check(sync ? provider.DeleteHolon(id.ToString()) : await provider.DeleteHolonAsync(id.ToString())); break;
            case 2: Check(sync ? provider.DeleteHolonWithSoftDelete(id, true) : await provider.DeleteHolonWithSoftDeleteAsync(id, true)); break;
            case 3: Check(sync ? provider.DeleteHolonWithSoftDelete(id, false) : await provider.DeleteHolonWithSoftDeleteAsync(id, false)); break;
            case 4: Check(sync ? provider.DeleteHolonWithSoftDelete(id.ToString(), true) : await provider.DeleteHolonWithSoftDeleteAsync(id.ToString(), true)); break;
            default: Check(sync ? provider.DeleteHolonWithSoftDelete(id.ToString(), false) : await provider.DeleteHolonWithSoftDeleteAsync(id.ToString(), false)); break;
        }
    }
}
