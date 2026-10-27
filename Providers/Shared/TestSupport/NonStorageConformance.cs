using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Enums;

namespace NextGenSoftware.OASIS.Providers.TestSupport;

/// <summary>Small capability gate for maps, AI, indexers and identity services.
/// A factory supplies a service-specific, read-only SDK/API probe; no storage CRUD or paid generation.</summary>
public static class NonStorageConformance
{
    public const string Suite="capability-v1";
    public sealed record Probe(string ServiceHost,string Operation,int RoundTrips,bool ReadOnly,bool Acknowledged);
    public sealed class Report
    {
        public string SuiteVersion {get;set;}=Suite;
        public string Provider {get;set;}="";
        public string Mode {get;set;}="";
        public List<string> Passed {get;set;}=new();
        public string CurrentCase {get;set;}="initial";
        public string FailureType {get;set;}="";
        public Probe Observation {get;set;}
        public bool Succeeded {get;set;}
    }
    public static async Task<Report> RunAsync(IOASISProvider provider,string expectedName,string mode,
        Func<Task<Probe>> readOnlyServiceProbe,Func<Report,Task> checkpoint)
    {
        ArgumentNullException.ThrowIfNull(provider);ArgumentNullException.ThrowIfNull(readOnlyServiceProbe);ArgumentNullException.ThrowIfNull(checkpoint);
        if(mode is not ("controlled" or "native"))throw new ArgumentException("Explicit execution mode required.",nameof(mode));
        var r=new Report{Provider=expectedName,Mode=mode};
        async Task Step(string name,Func<Task> action) {r.CurrentCase=name;await checkpoint(r);await action();r.Passed.Add(name);await checkpoint(r);}
        try
        {
            await Step("identity",()=> {Require(provider.ProviderName==expectedName&&provider.ProviderType!=null&&provider.ProviderType.Value is not (ProviderType.None or ProviderType.Default or ProviderType.All),"provider identity");return Task.CompletedTask;});
            await Step("capabilities",()=> {
                Require(provider.ProviderCapabilities?.Count>0,"declared capability");
                Require(!provider.ProviderCapabilities.Any(c=>c.Value is ProviderCategory.Storage or ProviderCategory.StorageLocal or ProviderCategory.StorageAndNetwork or ProviderCategory.StorageLocalAndNetwork),"capability-only provider advertises storage");
                return Task.CompletedTask;});
            await Step("activate",async()=> {var x=await provider.ActivateProviderAsync();Require(x!=null&&!x.IsError&&x.Result,"activation acknowledgement");});
            await Step("read-only-service-probe",async()=> {
                var x=await readOnlyServiceProbe();
                Require(x!=null&&x.ReadOnly&&x.Acknowledged&&x.RoundTrips>0&&!string.IsNullOrWhiteSpace(x.Operation)&&Regex.IsMatch(x.Operation,"^[a-zA-Z][a-zA-Z0-9._-]{0,80}$"),"confirmed read-only service response");
                // Store a host only, never a URL query carrying API keys or tokens.
                Require(!string.IsNullOrWhiteSpace(x.ServiceHost)&&Uri.CheckHostName(x.ServiceHost)!=UriHostNameType.Unknown,"public service host identity");
                r.Observation=x; // Publish only validated, credential-free probe identity.
            });
            await Step("deactivate",async()=> {var x=await provider.DeActivateProviderAsync();Require(x!=null&&!x.IsError&&x.Result,"deactivation acknowledgement");});
            r.CurrentCase="complete";r.Succeeded=true;await checkpoint(r);
        }
        catch(Exception error) {r.FailureType=error.GetType().FullName??error.GetType().Name;await checkpoint(r);}
        return r;
    }
    private static void Require(bool value,string contract) {if(!value)throw new InvalidOperationException("Capability conformance failed: "+contract);}
}
