using System;
using System.IO;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Holons;

namespace NextGenSoftware.OASIS.API.Providers.AzureStorageOASIS.UnitTests;

[TestClass]
public class AzureIntegrityTests
{
    [DataTestMethod]
    [DataRow(typeof(Avatar))] [DataRow(typeof(AvatarDetail))] [DataRow(typeof(Holon))]
    public void MalformedCanonicalJsonRetainsCauseAsTypedIntegrityFailure(Type recordType)
    {
        var method=typeof(AzureStorageOASIS).GetMethod("Deserialise",BindingFlags.Static|BindingFlags.NonPublic)!.MakeGenericMethod(recordType);
        var wrapper=Assert.ThrowsException<TargetInvocationException>(()=>method.Invoke(null,new object[] {"{ invalid canonical JSON"}));
        Assert.IsInstanceOfType(wrapper.InnerException,typeof(InvalidDataException));
        Assert.IsInstanceOfType(wrapper.InnerException!.InnerException,typeof(Newtonsoft.Json.JsonException));
    }
}
