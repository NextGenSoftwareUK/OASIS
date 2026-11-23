using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Serialization;

namespace NextGenSoftware.OASIS.API.Providers.SurrealDBOASIS.UnitTests
{
    /// <summary>Avatar details are written and read with the same canonical record codec.</summary>
    [TestClass]
    public class SurrealDbAvatarDetailCodecTests
    {
        [TestMethod]
        public void CanonicalDetailJsonRoundTrips()
        {
            var detail = new AvatarDetail { Id = Guid.NewGuid(), Karma = 29 };
            var loaded = StoredRecordJson.DeserializeStored<AvatarDetail>(StoredRecordJson.Serialize(detail));
            Assert.AreEqual(detail.Id, loaded.Id);
            Assert.AreEqual(detail.Karma, loaded.Karma);
        }
    }
}
