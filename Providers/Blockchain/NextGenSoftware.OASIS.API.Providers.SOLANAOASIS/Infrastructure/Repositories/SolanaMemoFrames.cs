using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Solnet.Wallet.Utilities;

namespace NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.Infrastructure.Repositories;

// Versioned transport framing only. Encryption, DTOs and native submission retain their existing owners.
internal sealed class SolanaMemoFrames
{
    internal const int TransactionBytesLimit=1232;
    internal const int PartCharacters=600;
    internal const int MaximumParts=256;
    internal const int MaximumMemoBytes=900;
    internal const string ChunkPrefix="oasis-solana-chunk-v1|";
    internal const string CommitPrefix="oasis-solana-commit-v1|";
    internal const string CipherPrefix="oasis-aes256gcm-";
    internal sealed record Commit(Guid WriteId,int Count,int Length,string Hash,string LastSignature);
    internal sealed record Chunk(Guid WriteId,int Index,int Count,int Length,string Hash,string PreviousSignature,string Data);
    private readonly string[] _parts;
    internal Guid WriteId {get;}
    internal int Count=>_parts.Length;
    internal int Length {get;}
    internal string Hash {get;}

    internal SolanaMemoFrames(string ciphertext)
    {
        RequireAscii(ciphertext,int.MaxValue);
        if(!ciphertext.StartsWith(CipherPrefix,StringComparison.Ordinal) || ciphertext.Contains('|'))
            throw new InvalidDataException("Only canonical encrypted memo ciphertext can be segmented.");
        if(ciphertext.Length>PartCharacters*MaximumParts)
            throw new ArgumentOutOfRangeException(nameof(ciphertext),"Encrypted record exceeds the bounded memo frame capacity.");
        WriteId=Guid.NewGuid();Length=ciphertext.Length;
        Hash=Convert.ToHexString(SHA256.HashData(Encoding.ASCII.GetBytes(ciphertext)));
        _parts=ciphertext.Chunk(PartCharacters).Select(part=>new string(part)).ToArray();
    }

    internal string ChunkMemo(int index,string previousSignature)
    {
        if(index<0 || index>=Count)throw new ArgumentOutOfRangeException(nameof(index));
        if(index==0){if(previousSignature!=null)throw new InvalidDataException("First chunk must not have a predecessor.");}
        else RequireSignature(previousSignature);
        var memo=string.Join('|',ChunkPrefix.TrimEnd('|'),WriteId.ToString("N"),Number(index),Number(Count),Number(Length),Hash,previousSignature??"-",_parts[index]);
        RequireAscii(memo,MaximumMemoBytes);return memo;
    }

    internal string CommitMemo(string lastSignature)
    {
        RequireSignature(lastSignature);
        var memo=string.Join('|',CommitPrefix.TrimEnd('|'),WriteId.ToString("N"),Number(Count),Number(Length),Hash,lastSignature);
        RequireAscii(memo,MaximumMemoBytes);return memo;
    }

    internal static Commit ParseCommit(string memo)
    {
        RequireAscii(memo,MaximumMemoBytes);var fields=memo.Split('|');
        if(fields.Length!=6 || !memo.StartsWith(CommitPrefix,StringComparison.Ordinal))throw new InvalidDataException("Unsupported or malformed memo commit frame.");
        var id=ParseId(fields[1]);var count=ParseNumber(fields[2]);var length=ParseNumber(fields[3]);
        RequireBounds(count,length);RequireHash(fields[4]);RequireSignature(fields[5]);
        return new(id,count,length,fields[4],fields[5]);
    }

    internal static Chunk ParseChunk(string memo)
    {
        RequireAscii(memo,MaximumMemoBytes);var fields=memo.Split('|');
        if(fields.Length!=8 || !memo.StartsWith(ChunkPrefix,StringComparison.Ordinal))throw new InvalidDataException("Unsupported or malformed memo chunk frame.");
        var id=ParseId(fields[1]);var index=ParseNumber(fields[2]);var count=ParseNumber(fields[3]);var length=ParseNumber(fields[4]);
        RequireBounds(count,length);RequireHash(fields[5]);
        if(index<0 || index>=count || fields[7].Length!=Math.Min(PartCharacters,length-index*PartCharacters))throw new InvalidDataException("Memo chunk index or payload length differs from its admitted bounds.");
        var previous=fields[6]=="-"?null:fields[6];
        if(index==0){if(previous!=null)throw new InvalidDataException("First memo chunk has an unexpected predecessor.");}
        else RequireSignature(previous);
        return new(id,index,count,length,fields[5],previous,fields[7]);
    }

    internal static string Join(Commit commit,IReadOnlyList<(string Signature,string Memo)> chunks)
    {
        ArgumentNullException.ThrowIfNull(commit);ArgumentNullException.ThrowIfNull(chunks);
        RequireBounds(commit.Count,commit.Length);RequireHash(commit.Hash);RequireSignature(commit.LastSignature);
        if(commit.WriteId==Guid.Empty || chunks.Count!=commit.Count)throw new InvalidDataException("Incomplete memo chunk set.");
        var seen=new HashSet<string>(StringComparer.Ordinal);var payload=new StringBuilder(commit.Length);string previous=null;
        for(var index=0;index<chunks.Count;index++){
            var row=chunks[index];RequireSignature(row.Signature);
            if(!seen.Add(row.Signature))throw new InvalidDataException("Repeated memo chunk signature.");
            var frame=ParseChunk(row.Memo);
            if(frame.WriteId!=commit.WriteId || frame.Index!=index || frame.Count!=commit.Count || frame.Length!=commit.Length
                || frame.Hash!=commit.Hash || frame.PreviousSignature!=previous)throw new InvalidDataException("Memo chunk ownership, sequence or manifest binding differs.");
            payload.Append(frame.Data);previous=row.Signature;
        }
        if(previous!=commit.LastSignature || payload.Length!=commit.Length)throw new InvalidDataException("Memo commit does not identify this complete chunk chain.");
        var encoded=payload.ToString();
        if(!encoded.StartsWith(CipherPrefix,StringComparison.Ordinal)
            || !CryptographicOperations.FixedTimeEquals(Convert.FromHexString(commit.Hash),SHA256.HashData(Encoding.ASCII.GetBytes(encoded))))
            throw new InvalidDataException("Reconstructed encrypted memo digest differs from its commit.");
        return encoded;
    }

    private static void RequireBounds(int count,int length)
    {
        if(count<1 || count>MaximumParts || length<1 || length>PartCharacters*MaximumParts || count!=(length+PartCharacters-1)/PartCharacters)
            throw new InvalidDataException("Memo frame count or payload size exceeds its canonical bounds.");
    }
    private static void RequireAscii(string value,int maximum)
    {
        if(string.IsNullOrEmpty(value) || value.Length>maximum || value.Any(character=>character>127))throw new InvalidDataException("Memo framing requires bounded ASCII.");
    }
    private static string Number(int value)=>value.ToString(CultureInfo.InvariantCulture);
    private static int ParseNumber(string value)
    {
        if(!int.TryParse(value,NumberStyles.None,CultureInfo.InvariantCulture,out var number) || value!=Number(number))throw new InvalidDataException("Noncanonical memo frame integer.");
        return number;
    }
    private static Guid ParseId(string value)
    {
        if(!Guid.TryParseExact(value,"N",out var id) || id==Guid.Empty || value!=id.ToString("N"))throw new InvalidDataException("Invalid memo write identity.");
        return id;
    }
    private static void RequireHash(string value)
    {
        if(value==null || value.Length!=64 || value.Any(character=>!char.IsAsciiHexDigitUpper(character)))throw new InvalidDataException("Invalid canonical memo payload digest.");
    }
    private static void RequireSignature(string value)
    {
        try {if(string.IsNullOrWhiteSpace(value) || Encoders.Base58.DecodeData(value).Length!=64 || Encoders.Base58.EncodeData(Encoders.Base58.DecodeData(value))!=value)throw new FormatException();}
        catch(Exception cause) when(cause is ArgumentException or FormatException){throw new InvalidDataException("Invalid native memo signature reference.",cause);}
    }
}
