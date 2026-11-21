using System;
using System.Threading.Tasks;
using NextGenSoftware.Holochain.HoloNET.Client;
using NextGenSoftware.Holochain.HoloNET.Client.Data.Admin.Requests.Objects;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Cql;

internal sealed class HoloOasisReadSigner : IZomeCallSigner
{
    private readonly byte[] _dnaHash;
    private readonly byte[] _agentKey;
    private readonly string _zome;
    private readonly SigningCredentials _credentials;

    public HoloOasisReadSigner(byte[] dnaHash, byte[] agentKey, string zome, SigningCredentials credentials)
    {
        ArgumentNullException.ThrowIfNull(dnaHash);
        ArgumentNullException.ThrowIfNull(agentKey);
        ArgumentException.ThrowIfNullOrWhiteSpace(zome);
        ArgumentNullException.ThrowIfNull(credentials);
        _dnaHash = (byte[])dnaHash.Clone();
        _agentKey = (byte[])agentKey.Clone();
        _zome = zome;
        _credentials = credentials;
    }

    public Task<ZomeCallParamsSigned> SignZomeCallAsync(ZomeCallToSign call)
    {
        ArgumentNullException.ThrowIfNull(call);
        if (!string.Equals(call.ZomeName, _zome, StringComparison.Ordinal) ||
            !string.Equals(call.FnName, "get_holon_by_id", StringComparison.Ordinal) ||
            !_dnaHash.AsSpan().SequenceEqual(call.CellIdDnaHash) ||
            !_agentKey.AsSpan().SequenceEqual(call.CellIdAgentPubKey))
        {
            throw new InvalidOperationException("The HoloOASIS CQL signing grant only permits exact holon reads on the configured cell.");
        }

        return Task.FromResult(HoloNETClientAppBase.SignZomeCallLocally(call, _credentials));
    }
}
