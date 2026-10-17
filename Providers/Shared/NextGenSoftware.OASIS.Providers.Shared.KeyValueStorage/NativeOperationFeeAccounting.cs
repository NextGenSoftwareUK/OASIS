using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text.RegularExpressions;

namespace NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

/// <summary>A caller-supplied valuation of one native denomination's net fee.
/// The recovery host must independently verify native evidence and authorize the valuation source.</summary>
public sealed record NativeFeeValuation(Guid OperationId,string Provider,string ProviderRequestId,
    string Denomination,string NativeNetUnits,decimal NetCostUsd,string ValuationSource,
    string ValuationVersion,DateTime ValuedAtUtc);

public sealed record NativeOperationFeeTotal(Guid OperationId,decimal FeesUsd,decimal RebatesUsd,
    decimal ChargeUsd,decimal UnusedRebateUsd,IReadOnlyList<NativeFeeValuation> Evidence);

/// <summary>Pure operation-scoped accounting. Does not fetch prices, verify receipts, authorize spending,
/// persist evidence, settle a reservation, or carry excess rebates to another operation.</summary>
public static class NativeOperationFeeAccounting
{
    public static NativeOperationFeeTotal Calculate(Guid operationId,IEnumerable<NativeFeeValuation> valuations)
    {
        if(operationId==Guid.Empty)throw new ArgumentException("An admitted operation identity is required.",nameof(operationId));
        ArgumentNullException.ThrowIfNull(valuations);
        var rows=valuations.Take(1001).ToArray();
        if(rows.Length==0||rows.Length>1000)throw new ArgumentException("Between one and 1000 valued native fee components are required.",nameof(valuations));
        var keys=new HashSet<(string Provider,string Receipt,string Denomination)>();
        decimal fees=0,rebates=0;
        foreach(var row in rows){
            if(row==null||row.OperationId!=operationId)throw new ArgumentException("Every native fee valuation must belong to this same operation.",nameof(valuations));
            static bool Bounded(string value,int length)=>!string.IsNullOrWhiteSpace(value)&&value.Length<=length&&!value.Any(char.IsControl);
            if(!Bounded(row.Provider,128)||!Bounded(row.ProviderRequestId,256)||!Bounded(row.Denomination,128)
                ||!Bounded(row.ValuationSource,128)||!Bounded(row.ValuationVersion,128)
                ||row.ValuedAtUtc.Kind!=DateTimeKind.Utc||row.ValuedAtUtc==DateTime.MinValue)
                throw new ArgumentException("Native identity, denomination and explicit UTC valuation provenance are required.",nameof(valuations));
            if(row.NativeNetUnits==null||row.NativeNetUnits.Length>80
                ||!Regex.IsMatch(row.NativeNetUnits,@"\A(?:0|-?[1-9][0-9]*)\z"))
                throw new ArgumentException("Exact canonical signed native fee units are required.",nameof(valuations));
            var native=BigInteger.Parse(row.NativeNetUnits,System.Globalization.CultureInfo.InvariantCulture);
            if(native.Sign!=Math.Sign(row.NetCostUsd))
                throw new ArgumentException("Valuation must preserve the native fee/rebate sign without rounding nonzero evidence to zero.",nameof(valuations));
            if(!keys.Add((row.Provider,row.ProviderRequestId,row.Denomination)))
                throw new ArgumentException("A native receipt denomination cannot be counted twice.",nameof(valuations));
            checked{if(row.NetCostUsd>=0)fees+=row.NetCostUsd;else rebates-=row.NetCostUsd;}
        }
        var net=checked(fees-rebates);
        return new(operationId,fees,rebates,Math.Max(0,net),Math.Max(0,-net),Array.AsReadOnly(rows));
    }
}
