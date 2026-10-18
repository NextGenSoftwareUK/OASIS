using System;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Holons;

namespace NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.UnitTests;

[TestClass][DoNotParallelize]
public class MetadataCultureTests
{
    [DataTestMethod][DataRow("en-US")][DataRow("de-DE")][DataRow("fr-FR")]
    public async Task MetadataLookupUsesInvariantNumericText(string culture)
    {
        var previous=CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture=CultureInfo.GetCultureInfo(culture);
            var provider=new TestProvider(new InMemoryBackend());
            var root=new Holon{Id=Guid.NewGuid(),Name="owned invariant metadata"};root.MetaData["amount"]=2.5m;
            Assert.IsFalse((await provider.SaveHolonAsync(root,saveChildren:false)).IsError);
            var result=await provider.LoadHolonsByMetaDataAsync("amount","2.5",loadChildren:false);
            Assert.IsFalse(result.IsError,result.Message);Assert.AreEqual(root.Id,result.Result.Single().Id);
        }
        finally{CultureInfo.CurrentCulture=previous;}
    }
}
