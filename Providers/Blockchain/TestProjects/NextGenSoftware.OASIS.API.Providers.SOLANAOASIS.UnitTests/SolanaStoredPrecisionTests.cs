using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.Entities.Models;
using NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.Infrastructure.Repositories;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.UnitTests;

[TestClass]
public sealed class SolanaStoredPrecisionTests
{
    [DataTestMethod][DataRow(0)][DataRow(1)][DataRow(2)]
    public void TypedStoredMemoRetainsExactMetadata(int kind)
    {
        const decimal precise=0.1234567890123456789012345678m;
        const string numericText="0.1234567890123456789012345678";
        const string literalDate="2026-10-20T10:11:12.1234567+02:00";
        SolanaBaseDto source=kind==0?new SolanaAvatarDto():kind==1?new SolanaAvatarDetailDto():new SolanaHolonDto();
        source.Id=Guid.NewGuid();source.Version=7;
        var property=source.GetType().GetProperty("MetaData")!;
        property.SetValue(source,new Dictionary<string,object>{["precise"]=precise,["wide"]=1e100,["numeric_text"]=numericText,["literal_date"]=literalDate});
        var method=typeof(SolanaRepository).GetMethod("DeserializeStoredRecord",BindingFlags.Static|BindingFlags.NonPublic)!.MakeGenericMethod(source.GetType());
        var result=(SolanaBaseDto)method.Invoke(null,new object[]{OasisJson.Serialize(source)})!;
        var metadata=(Dictionary<string,object>)property.GetValue(result)!;
        Assert.AreEqual(precise,Convert.ToDecimal(metadata["precise"],System.Globalization.CultureInfo.InvariantCulture));
        Assert.AreEqual(1e100,Convert.ToDouble(metadata["wide"],System.Globalization.CultureInfo.InvariantCulture));
        Assert.IsInstanceOfType<string>(metadata["numeric_text"]);Assert.AreEqual(numericText,metadata["numeric_text"]);
        Assert.IsInstanceOfType<string>(metadata["literal_date"]);Assert.AreEqual(literalDate,metadata["literal_date"]);
        Assert.AreEqual(source.Id,result.Id);Assert.AreEqual(7,result.Version);
    }
}
