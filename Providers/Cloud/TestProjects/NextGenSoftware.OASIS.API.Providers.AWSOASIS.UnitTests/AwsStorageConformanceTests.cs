using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.OASISBootLoader;
using NextGenSoftware.OASIS.Providers.TestSupport;

namespace NextGenSoftware.OASIS.API.Providers.AWSOASIS.UnitTests;

// Opt-in native binding to one canonical suite; no new runtime, routing or recovery implementation.
[TestClass,DoNotParallelize]
public class AwsStorageConformanceTests
{
    [DataTestMethod]
    [DataRow("AWSOASIS",false)] [DataRow("AWSOASIS",true)]
    [DataRow("DynamoDBOASIS",false)] [DataRow("DynamoDBOASIS",true)]
    public Task SharedDynamoStorageInterface(string name,bool sync)=>Run(name,sync);

    [DataTestMethod] [DataRow(false)] [DataRow(true)]
    public Task SharedS3StorageInterface(bool sync)=>Run("AWSS3OASIS",sync);

    private async Task Run(string name,bool sync)
    {
        var root=new DirectoryInfo(AppContext.BaseDirectory);
        while(root!=null&&!Directory.Exists(Path.Combine(root.FullName,"Scripts","TestHosts")))root=root.Parent;
        Assert.IsNotNull(root);
        var work=Path.Combine(root.FullName,"TestResults","ProviderConformanceCurrent",name,sync?"sync":"async");
        Directory.CreateDirectory(work);var reportPath=Path.Combine(work,"report.json");
        Assert.IsFalse(File.Exists(reportPath)||File.Exists(reportPath+".next"),"Preserve previous fixture/report before another run.");
        using var lease=new FileStream(Path.Combine(work,"environment.lock"),FileMode.OpenOrCreate,FileAccess.ReadWrite,FileShare.None);
        async Task Checkpoint(CoreStorageConformance.Report report)
        {
            await using(var file=new FileStream(reportPath+".next",FileMode.CreateNew,FileAccess.Write,FileShare.None))
            {await JsonSerializer.SerializeAsync(file,report);file.Flush(true);}
            File.Move(reportPath+".next",reportPath,overwrite:true);
        }
        IOASISStorageProvider provider;
        try
        {
            static string Read(string variable)=>Environment.GetEnvironmentVariable(variable) is string value&&!string.IsNullOrWhiteSpace(value)
                ? value : throw new InvalidOperationException("Explicit conformance configuration is missing.");
            if(name=="AWSS3OASIS")
            {
                if(Read("OASIS_S3_CONFORMANCE_ALLOW_BILLABLE_TEST_REQUESTS")!="1")
                    throw new InvalidOperationException("Existing isolated S3 test bucket and explicit request-cost opt-in required.");
                var keyVariable=Read("OASIS_S3_CONFORMANCE_STORAGE_KEY_VARIABLE");
                _=Read(keyVariable); // Reference a persisted key; never generate or publish it.
                provider=AwsS3ProviderFactory.Create(new AWSS3OASISSettings{
                    Region=Read("OASIS_S3_CONFORMANCE_REGION"),BucketName=Read("OASIS_S3_CONFORMANCE_BUCKET"),
                    KeyPrefix="oasis-conformance/",UseDefaultCredentials=true,
                    StorageEncryptionKeyEnvironmentVariable=keyVariable},null!);
            }
            else
            {
                var endpoint=Read("DYNAMODB_SERVICE_URL");
                if(endpoint!="http://127.0.0.1:18000")throw new InvalidOperationException("Retained loopback DynamoDB Local required; no public fallback.");
                // DynamoDB Local requires an alphanumeric access key; these are local-only fixtures.
                const string testKey="localtestkey",testSecret="localtestsecret";
                provider=name=="AWSOASIS"
                    ?CloudProviderFactory.CreateAws(new AWSOASISProviderSettings{Region="us-east-1",AccessKey=testKey,SecretKey=testSecret,ServiceUrl=endpoint},null!)
                    :new NextGenSoftware.OASIS.API.Providers.DynamoDBOASIS.DynamoDBOASIS(testKey,testSecret,"us-east-1",endpoint);
            }
        }
        catch(Exception failure)
        {
            await Checkpoint(new CoreStorageConformance.Report{Provider=name,Mode="native",Synchronous=sync,CurrentCase="configuration",FailureType=failure.GetType().FullName!});
            Assert.Fail("AWS configuration blocked this binding; no native storage operation was dispatched. See the matrix report.");return;
        }
        try
        {
            var result=await CoreStorageConformance.RunAsync(provider,name,"native",sync,Checkpoint,expectedDefaultDeletion:name=="AWSS3OASIS"?null:"absent");
            Assert.IsTrue(result.Succeeded,result.CurrentCase+": "+result.FailureType);
        }
        finally {if(provider is IDisposable disposable)disposable.Dispose();}
    }
}
