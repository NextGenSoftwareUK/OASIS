using System.Reflection;
using System.Security.Cryptography;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Helpers;

namespace NextGenSoftware.OASIS.API.Providers.NEAROASIS.UnitTests;

[TestClass][DoNotParallelize]
public class NearStorageMutationOutcomeTests
{
    public static IEnumerable<object[]> Cases(){foreach(var delete in new[]{false,true})foreach(var scenario in Enumerable.Range(0,12))yield return new object[]{delete,scenario};}
    [DataTestMethod][DynamicData(nameof(Cases),DynamicDataSourceType.Method)]
    public async Task OrdinaryStorageRequiresSameBoundFinalMutationOwner(bool delete,int scenario)
    {
        var names=new[]{"OASIS_NEAR_STORAGE_ENCRYPTION_KEY","OASIS_NEAR_STORAGE_MUTATION_CASE"};
        var previous=names.Select(Environment.GetEnvironmentVariable).ToArray();var key=RandomNumberGenerator.GetBytes(32);
        try {
            Environment.SetEnvironmentVariable(names[0],Convert.ToBase64String(key));
            Environment.SetEnvironmentVariable(names[1],scenario.ToString());
            var type=typeof(NEAROASIS).Assembly.GetType("NextGenSoftware.OASIS.API.Providers.NEAROASIS.NearSdkService")!;
            var service=Activator.CreateInstance(type,new object[]{"http://127.0.0.1:1","fixture","contract.near","sender.near",
                "controlled-key","node",Path.Combine(AppContext.BaseDirectory,"Fixtures","storage-mutation-outcome.mjs")})!;
            var args=delete?new object[]{"holon:11111111-1111-1111-1111-111111111111"}:
                new object[]{"holon:11111111-1111-1111-1111-111111111111","private-plaintext"};
            var task=(Task<string>)type.GetMethod(delete?"DeleteAsync":"PutAsync")!.Invoke(service,args)!;
            if(scenario==0){Assert.AreEqual("original-storage-transaction",await task);return;}
            Exception? failure=null;try{await task;}catch(Exception ex){failure=ex;}
            Assert.IsNotNull(failure,"Hash alone must not acknowledge storage success.");
            Assert.AreEqual(scenario==1?"NearMutationRejectedException":"NearMutationUncertainException",failure.GetType().Name);
            Assert.AreEqual(scenario!=1,NativeMutationReconciliation.IsRequired(failure));
            Assert.AreEqual(scenario==11?string.Empty:"original-storage-transaction",failure.GetType().GetProperty("OriginalTransactionId")!.GetValue(failure));
        } finally {for(var i=0;i<names.Length;i++)Environment.SetEnvironmentVariable(names[i],previous[i]);CryptographicOperations.ZeroMemory(key);}
    }
}
