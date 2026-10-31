using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.ProviderTestSupport;

namespace NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.UnitTests;

[TestClass]
public sealed class AllVersionExportVisibilityTests
{
    private sealed class Records(IReadOnlyList<IHolon> rows):KeyValueStorageProviderBase(new InMemoryBackend())
    {
        public override Task<OASISResult<IEnumerable<IHolon>>> ExportAllAsync(int version=0)
        {Assert.AreEqual(-1,version);return Task.FromResult(new OASISResult<IEnumerable<IHolon>>{Result=rows,IsLoaded=true,LoadedCount=rows.Count});}
    }
    [DataTestMethod][DataRow(false)][DataRow(true)]
    public Task AllVersionOwnedExportsPruneEachStoredGraphIndependently(bool sync)
        =>AllVersionExportVisibilityAssertions.Verify(async(rows,owner)=>{
            var provider=new Records(rows);
            return sync?provider.ExportAllDataForAvatarById(owner,-1):await provider.ExportAllDataForAvatarByIdAsync(owner,-1);
        });
}
