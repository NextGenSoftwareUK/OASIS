using System;
using System.IO;
using System.Linq;
using System.Globalization;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Holons;

namespace NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.UnitTests;

[TestClass]
public class StoredRecordLocationTests
{
    [DataTestMethod]
    [DataRow(90000001L,0L,1)] [DataRow(-90000001L,0L,1)]
    [DataRow(0L,180000001L,1)] [DataRow(0L,-180000001L,1)] [DataRow(0L,0L,-1)]
    public void InvalidRequestCannotBecomeAQuery(long latitude,long longitude,int radius)
        => Assert.ThrowsException<ArgumentOutOfRangeException>(()=>new StoredRecordLocationQuery(latitude,longitude,radius));

    [DataTestMethod]
    [DataRow(0L,0L,0,0d,0d,true)]
    [DataRow(0L,0L,100,0.0005d,0d,true)] [DataRow(0L,0L,100,0.001d,0d,false)]
    [DataRow(0L,179999500L,200,0d,-179.9995d,true)]
    [DataRow(0L,180000000L,0,0d,-180d,true)] [DataRow(90000000L,37000000L,0,90d,-175d,true)]
    public void RadiusAndCanonicalBoundariesPreserveOriginalRecords(long latitude,long longitude,int radius,double storedLatitude,double storedLongitude,bool expected)
    {
        var record=new Holon {Id=Guid.NewGuid(),Name="preserved full record",Version=7};
        record.MetaData["Latitude"]=storedLatitude;record.MetaData["Longitude"]=storedLongitude;
        var query=new StoredRecordLocationQuery(latitude,longitude,radius);var found=query.Select(new[] {record});
        Assert.AreEqual(expected ? 1:0,found.Count);if(expected) Assert.AreSame(record,found[0]);
        Assert.AreEqual(7,record.Version);Assert.AreEqual(storedLongitude,record.MetaData["Longitude"]);
    }

    [DataTestMethod]
    [DataRow("missing-pair")] [DataRow("nan")] [DataRow("infinite")] [DataRow("range")]
    public void InvalidStoredCoordinatesAreIntegrityFailures(string kind)
    {
        var record=new Holon();record.MetaData["Latitude"]=kind=="nan" ? "NaN":kind=="infinite" ? "Infinity":kind=="range" ? "91":"0";
        if(kind!="missing-pair") record.MetaData["Longitude"]="0";
        Assert.ThrowsException<InvalidDataException>(()=>new StoredRecordLocationQuery(0,0,100).Select(new[] {record}));
    }

    [TestMethod]
    public void MissingLocationIsOptionalAndParsingIsCultureIndependent()
    {
        var culture=CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture=CultureInfo.GetCultureInfo("fr-FR");
            var located=new Holon();located.MetaData["Latitude"]="-33.0009";located.MetaData["Longitude"]="151";
            var result=new StoredRecordLocationQuery(-33000000,151000000,200).Select(new[] {new Holon(),located});
            Assert.AreSame(located,result.Single());
        }
        finally {CultureInfo.CurrentCulture=culture;}
    }
}
