using System;
using System.Net.Http;
using System.Security.Cryptography;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.API.Providers.SOLIDOASIS;

namespace NextGenSoftware.OASIS.OASISBootLoader;

internal static class SolidProviderFactory
{
    internal static SOLIDOASIS Create(SolidOASISProviderSettings settings,string overrideConnectionString=null)
    {
        ArgumentNullException.ThrowIfNull(settings);
        if(!Uri.TryCreate(settings.PodRoot,UriKind.Absolute,out var root))
            throw new ArgumentException("An explicit SOLID Pod root is required.",nameof(settings));
        root=SolidPrivateRecordCodec.NormalizePodRoot(root);
        if(overrideConnectionString!=null){
            if(!Uri.TryCreate(overrideConnectionString,UriKind.Absolute,out var requested)
                ||SolidPrivateRecordCodec.NormalizePodRoot(requested)!=root)
                throw new ArgumentException("SOLID connection override cannot change the configured Pod credential scope.",nameof(overrideConnectionString));
        }
        var mode=settings.AuthenticationMode;
        var dpop=string.Equals(mode,"DpopClientCredentials",StringComparison.OrdinalIgnoreCase);
        var bearer=string.Equals(mode,"Bearer",StringComparison.OrdinalIgnoreCase);
        if(!dpop&&!bearer&&!string.Equals(mode,"Anonymous",StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("SOLID authentication mode must be DpopClientCredentials, Bearer or Anonymous.",nameof(settings));
        if(!dpop&&(!string.IsNullOrWhiteSpace(settings.ClientId)||!string.IsNullOrWhiteSpace(settings.TokenEndpoint)))
            throw new ArgumentException("SOLID issuer/client settings require DpopClientCredentials mode.",nameof(settings));
        Uri tokenEndpoint=null;string secret=null;
        if(dpop){
            if(string.IsNullOrWhiteSpace(settings.ClientId)||!Uri.TryCreate(settings.TokenEndpoint,UriKind.Absolute,out tokenEndpoint))
                throw new ArgumentException("SOLID DPoP requires explicit client ID and token endpoint.",nameof(settings));
            secret=RequiredEnvironment(settings.ClientSecretEnvironmentVariable,"client secret");
        }
        var token=bearer?RequiredEnvironment(settings.BearerTokenEnvironmentVariable,"bearer token"):string.Empty;
        byte[] key;
        try{key=Convert.FromBase64String(RequiredEnvironment(settings.StorageKeyEnvironmentVariable,"storage key"));}
        catch(FormatException cause){throw new ArgumentException("SOLID storage key must be base64-encoded.",nameof(settings),cause);}
        HttpMessageHandler handler=null;
        try{
            handler=dpop?new SolidDpopClientCredentialsHandler(root,tokenEndpoint,settings.ClientId,secret):new HttpClientHandler();
            return new SOLIDOASIS(root.AbsoluteUri,token,key,handler){IsVersionControlEnabled=settings.IsVersionControlEnabled};
        }catch{handler?.Dispose();throw;}
        finally{CryptographicOperations.ZeroMemory(key);}
    }

    private static string RequiredEnvironment(string name,string purpose)
    {
        if(string.IsNullOrWhiteSpace(name))throw new ArgumentException($"SOLID {purpose} environment variable name is required.");
        var value=Environment.GetEnvironmentVariable(name);
        if(string.IsNullOrWhiteSpace(value))throw new ArgumentException($"Configured SOLID {purpose} environment variable is missing.");
        return value;
    }
}
