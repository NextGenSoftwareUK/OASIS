using NextGenSoftware.OASIS.API.Native.EndPoint;
using NextGenSoftware.OASIS.STAR.DNA;
using Xunit;

namespace NextGenSoftware.OASIS.STAR.WebAPI.IntegrationTests
{
    public class OASISFixture : IDisposable
    {
        public STARAPI StarAPI { get; } = new STARAPI(new STARDNA());

        public void Dispose() { }
    }

    [CollectionDefinition("OASISBoot")]
    public class OASISCollection : ICollectionFixture<OASISFixture> { }
}
