using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Google.Protobuf;
using TronNet.Protocol;

namespace NextGenSoftware.OASIS.API.Providers.TRONOASIS;

/// <summary>Validates authority in a caller-supplied verified account snapshot; never invents on-chain permissions.</summary>
public static class TronPermissionVerifier
{
    public static void Verify(Account account,ByteString expectedOwner,int permissionId,int contractType,IReadOnlyCollection<ByteString> recoveredSigners)
    {
        if(account==null||expectedOwner==null||expectedOwner.Length!=21||expectedOwner[0]!=0x41||!account.Address.Equals(expectedOwner))
            throw new InvalidDataException("TRON permission account does not match trusted operation owner.");
        if(permissionId<0||permissionId==1||contractType<0||contractType>255||!Enum.IsDefined(typeof(Transaction.Types.Contract.Types.ContractType),contractType))
            throw new InvalidDataException("TRON transaction permission or operation selector is invalid.");
        Permission permission;
        if(permissionId==0){
            permission=account.OwnerPermission;
            // java-tron 4.8.2 AccountCapsule.getPermissionById(0): absent owner message means protocol default, not absent authority.
            if(permission==null){
                permission=new Permission{Id=0,Threshold=1,PermissionName="owner"};
                permission.Keys.Add(new Key{Address=account.Address,Weight=1});
            }
        }
        else{
            var candidates=account.ActivePermission.Where(item=>item.Id==permissionId).ToArray();
            if(candidates.Length!=1)throw new InvalidDataException("One exact active TRON permission required.");
            permission=candidates[0];
        }
        if(permission==null||permission.Id!=permissionId||(int)permission.Type!=(permissionId==0?0:2)||permission.Threshold<=0||permission.Keys.Count==0)
            throw new InvalidDataException("TRON permission threshold/type/identity is not established.");
        if(permissionId!=0&&(permission.Operations.Length!=32||(permission.Operations[contractType/8]&(1<<(contractType%8)))==0))
            throw new InvalidDataException("TRON operation is not authorised by active permission mask.");
        var keys=new Dictionary<ByteString,long>();
        foreach(var key in permission.Keys){
            if(key.Address.Length!=21||key.Address[0]!=0x41||key.Weight<=0||!keys.TryAdd(key.Address,key.Weight))
                throw new InvalidDataException("TRON permission keys must be unique valid weighted addresses.");
        }
        if(recoveredSigners==null||recoveredSigners.Count==0)throw new InvalidDataException("Recovered TRON permission signers required.");
        var unique=new HashSet<ByteString>();long weight=0;
        foreach(var signer in recoveredSigners){
            if(signer==null||!unique.Add(signer)||!keys.TryGetValue(signer,out var value))
                throw new InvalidDataException("TRON signature key is duplicated or lacks permission authority.");
            weight=checked(weight+value);
        }
        if(weight<permission.Threshold)throw new InvalidDataException("TRON signature weight does not meet permission threshold.");
    }
}
