using System;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using Nethereum.RPC.Eth.DTOs;
using Nethereum.Util;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Utilities;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;
using Contracts=NextGenSoftware.OASIS.API.Providers.EthereumOASIS.ContractDefinition;

namespace NextGenSoftware.OASIS.API.Providers.EthereumOASIS;

public partial class EthereumOASIS
{
    private async Task<(TransactionReceipt Receipt,T Record)> UpsertStorageHistoryAsync<T>(string kind,T proposed,bool createHistory)
        where T:class,IHolonBase
    {
        ArgumentNullException.ThrowIfNull(proposed);
        var expected=kind switch {"avatar"=>typeof(Avatar),"avatar-detail"=>typeof(AvatarDetail),"holon"=>typeof(Holon),_=>null};
        if(expected!=typeof(T) || proposed.Id==Guid.Empty || proposed.Version<0)
            throw new ArgumentException("Ethereum checked history requires a matching concrete kind, explicit identity and nonnegative version.");
        // Freeze the entire caller-owned graph before the first native await.
        var snapshot=OasisJson.DeserializeStored<T>(SerializeCanonicalEmbeddedRecord(proposed).ToString(Newtonsoft.Json.Formatting.None));
        if(!IsProviderActivated || _nextGenSoftwareOasisService==null)throw new InvalidOperationException("Ethereum provider is not activated.");
        var key=new BigInteger(HashUtility.GetNumericHash(snapshot.Id.ToString()));
        var observed=await ReadHistorySnapshotAsync(kind,key);
        var present=observed.EntityId!=0 || !string.IsNullOrEmpty(observed.Identity) || !string.IsNullOrEmpty(observed.Info);
        var hash=present?Sha3Keccack.Current.CalculateHash(Encoding.UTF8.GetBytes(observed.Info??string.Empty)):new byte[32];
        var staged=await StageStorageHistoryAsync(kind,snapshot,observed.EntityId,observed.Identity,observed.Info,createHistory);
        Contracts.CheckedStorageFunction request=kind switch {
            "avatar"=>new Contracts.CreateAvatarCheckedFunction(),
            "avatar-detail"=>new Contracts.CreateAvatarDetailCheckedFunction(),
            _=>new Contracts.CreateHolonCheckedFunction()
        };
        request.EntityId=key;request.Identity=snapshot.Id.ToString();request.ExpectedPresent=present;
        request.ExpectedInfoHash=hash;request.Info=staged.Ciphertext;
        // Reuse the existing submission/receipt owner. No retry, second pre-read or caller publication.
        var receipt=request switch {
            Contracts.CreateAvatarCheckedFunction avatar=>await SubmitStorageRequestAsync(avatar),
            Contracts.CreateAvatarDetailCheckedFunction detail=>await SubmitStorageRequestAsync(detail),
            Contracts.CreateHolonCheckedFunction holon=>await SubmitStorageRequestAsync(holon),
            _=>throw new InvalidOperationException("Unknown checked Ethereum request.")
        };
        return (receipt,staged.Record);
    }

    private async Task<(BigInteger EntityId,string Identity,string Info)> ReadHistorySnapshotAsync(string kind,BigInteger key)
    {
        if(kind=="avatar"){
            var row=(await _nextGenSoftwareOasisService.GetAvatarByIdQueryAsync(key)).ReturnValue1;
            return (row.EntityId,row.AvatarId,row.Info);
        }
        if(kind=="avatar-detail"){
            var row=(await _nextGenSoftwareOasisService.GetAvatarDetailByIdQueryAsync(key)).ReturnValue1;
            return (row.EntityId,row.AvatarId,row.Info);
        }
        var holon=(await _nextGenSoftwareOasisService.GetHolonByIdQueryAsync(key)).ReturnValue1;
        return (holon.EntityId,holon.HolonId,holon.Info);
    }
}
