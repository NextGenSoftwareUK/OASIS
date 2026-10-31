using Xunit;
using NextGenSoftware.OASIS.API.Core.Managers;

namespace NextGenSoftware.OASIS.API.Core.UnitTests;

public class LevelLookupConcurrencyTests
{
    [Fact]
    public void ConcurrentRegenerationNeverPublishesPartialThresholds()
    {
        LevelManager.GenerateLevelLookup();var expected=LevelManager.GetLevelFromKarma(1000);
        Parallel.For(0,2000,new ParallelOptions{MaxDegreeOfParallelism=4},i=>{
            if(i%3==0)LevelManager.GenerateLevelLookup();
            else for(var read=0;read<10;read++)Assert.Equal(expected,LevelManager.GetLevelFromKarma(1000));
        });
    }
}
