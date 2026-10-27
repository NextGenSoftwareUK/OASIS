using System.Numerics;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.OASISBootLoader;
using NextGenSoftware.OASIS.Providers.TestSupport;

namespace NextGenSoftware.OASIS.API.Providers.Web3CoreOASIS.UnitTests;

// Opt-in breadth binding. The same canonical suite, shipping V2 factories, no new contracts or routing work.
[TestClass,DoNotParallelize]
public class EvmStorageConformanceTests
{
    [DataTestMethod]
    [DataRow("EthereumOASIS",false)] [DataRow("EthereumOASIS",true)]
    [DataRow("ArbitrumOASIS",false)] [DataRow("ArbitrumOASIS",true)]
    [DataRow("PolygonOASIS",false)] [DataRow("PolygonOASIS",true)]
    public async Task SharedStorageInterfaceAgainstConfiguredLocalEvm(string name,bool sync)
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
                ? value : throw new InvalidOperationException("Protected local EVM conformance configuration is missing.");
            var endpoint=new Uri(Read("OASIS_EVM_CONFORMANCE_RPC"));
            if(!endpoint.IsLoopback||endpoint.Scheme!="http"||endpoint.Query.Length!=0||endpoint.UserInfo.Length!=0)
                throw new InvalidOperationException("This native binding uses only the retained loopback test EVM, never public/mainnet funds.");
            var chain=BigInteger.Parse(Read("OASIS_EVM_CONFORMANCE_CHAIN_ID"));
            if(chain!=31337&&chain!=1337)throw new InvalidOperationException("Retained local test chain identity required.");
            var key=Read("OASIS_EVM_CONFORMANCE_PRIVATE_KEY");var contract=Read("OASIS_EVM_CONFORMANCE_CONTRACT");
            _=Read("OASIS_WEB3CORE_STORAGE_ENCRYPTION_KEY");
            provider=name=="EthereumOASIS"
                ? EthereumProviderFactory.CreateForMode(new EthereumOASISProviderSettings{ConnectionString=endpoint.AbsoluteUri,ChainPrivateKey=key,ContractAddress=contract,ChainId=checked((long)chain)},HyperDriveModes.V2,null!)
                : name=="ArbitrumOASIS"
                ? EvmProviderFactory.CreateArbitrum(new ArbitrumOASISProviderSettings{ConnectionString=endpoint.AbsoluteUri,ChainPrivateKey=key,ContractAddress=contract,ChainId=checked((long)chain)},HyperDriveModes.V2,null!)
                : EvmProviderFactory.CreatePolygon(new PolygonOASISProviderSettings{ConnectionString=endpoint.AbsoluteUri,ChainPrivateKey=key,ContractAddress=contract,ChainId=checked((long)chain)},null!);
        }
        catch(Exception failure)
        {
            await Checkpoint(new CoreStorageConformance.Report{Provider=name,Mode="native",Synchronous=sync,CurrentCase="configuration",FailureType=failure.GetType().FullName!});
            Assert.Fail("Local EVM configuration blocked this binding; no native storage operation was dispatched. See the matrix report.");return;
        }
        var result=await CoreStorageConformance.RunAsync(provider,name,"native",sync,Checkpoint);
        Assert.IsTrue(result.Succeeded,result.CurrentCase+": "+result.FailureType);
    }
}
