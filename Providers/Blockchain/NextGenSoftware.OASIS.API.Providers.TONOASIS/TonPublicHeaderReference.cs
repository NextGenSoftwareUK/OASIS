using System;
using System.Globalization;
using System.IO;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace NextGenSoftware.OASIS.API.Providers.TONOASIS;

/// <summary>
/// Exact identity of an explicitly selected masterchain block. No wallet or
/// signing material is accepted. A root hash alone is not a complete descriptor.
/// </summary>
public sealed record TonPublicHeaderReference
{
    public TonPublicHeaderReference(uint sequence, string rootHash, string fileHash)
    {
        if (sequence == 0)
            throw new ArgumentException("A positive masterchain sequence is required.", nameof(sequence));

        Sequence = sequence;
        RootHash = CanonicalHash(rootHash);
        FileHash = CanonicalHash(fileHash);
    }

    public uint Sequence { get; }
    public string RootHash { get; }
    public string FileHash { get; }

    public const string MasterchainShard = "-9223372036854775808";
    public const string GenesisRootHash = "F6OpKZKqvqeFp6CQmFomXNMfMj2EnaUSOXN+Mh+wVWk=";
    public const string GenesisFileHash = "XplPz01CXAps5qeSWUtxcyBfdAo5zVb1N979KLSKD24=";

    public static string CanonicalHash(string value)
    {
        if (value == null || !Regex.IsMatch(value, @"\A[A-Za-z0-9+/]{43}=\z"))
            throw new InvalidDataException("A canonical padded 32-byte TON hash is required.");

        byte[] bytes = Convert.FromBase64String(value);
        if (bytes.Length != 32 || Convert.ToBase64String(bytes) != value)
            throw new InvalidDataException("The TON hash is not canonical.");

        return value;
    }

    /// <summary>
    /// tonlib presents zeroStateIdExt as BlockIdExt with shard 0 and seqno 0.
    /// Validate that representation and pinned Mainnet hashes; never turn its
    /// placeholder shard into the shard of an actual masterchain block.
    /// </summary>
    public static void ValidateMainnetIdentity(JsonElement envelope)
    {
        JsonElement result = SuccessfulResult(envelope);
        JsonElement init = result.GetProperty("init");

        if (result.GetProperty("@type").GetString() != "blocks.masterchainInfo" ||
            init.GetProperty("@type").GetString() != "ton.blockIdExt" ||
            init.GetProperty("workchain").GetRawText() != "-1" ||
            init.GetProperty("shard").GetString() != "0" ||
            UInt32Token(init.GetProperty("seqno")) != 0 ||
            CanonicalHash(init.GetProperty("root_hash").GetString()) != GenesisRootHash ||
            CanonicalHash(init.GetProperty("file_hash").GetString()) != GenesisFileHash)
            throw new InvalidDataException("The TON Mainnet zero-state identity differs.");
    }

    /// <summary>Validate full native identity before exposing any selected row.</summary>
    public JsonElement ValidateHeader(JsonElement envelope)
    {
        JsonElement header = SuccessfulResult(envelope);
        ValidateBlockIdentity(header.GetProperty("id"), Sequence, RootHash, FileHash);

        if (header.GetProperty("@type").GetString() != "blocks.header" ||
            header.GetProperty("global_id").GetRawText() != "-239")
            throw new InvalidDataException("The TON header type or network differs.");

        JsonElement parents = header.GetProperty("prev_blocks");
        if (parents.ValueKind != JsonValueKind.Array || parents.GetArrayLength() != 1)
            throw new InvalidDataException("A masterchain header must have one predecessor.");

        JsonElement parent = parents[0];
        ValidateBlockIdentity(parent, Sequence - 1,
            CanonicalHash(parent.GetProperty("root_hash").GetString()),
            CanonicalHash(parent.GetProperty("file_hash").GetString()));

        ulong start = LogicalTime(header.GetProperty("start_lt"));
        ulong end = LogicalTime(header.GetProperty("end_lt"));
        if (start > end)
            throw new InvalidDataException("The TON logical-time interval is reversed.");

        // tonlib exposes this hash fragment as signed int32. Preserve its wire
        // value; a negative hash fragment is not a negative counter or balance.
        Int32Token(header.GetProperty("validator_list_hash_short"));
        foreach (string field in new[] { "version", "gen_utime",
            "catchain_seqno", "min_ref_mc_seqno", "prev_key_block_seqno" })
            UInt32Token(header.GetProperty(field));

        foreach (string field in new[] { "after_merge", "after_split", "before_split",
            "want_merge", "want_split", "is_key_block" })
            header.GetProperty(field).GetBoolean();

        return header.Clone();
    }

    public static ulong LogicalTime(JsonElement value)
    {
        if (value.ValueKind != JsonValueKind.String)
            throw new InvalidDataException("A TON logical time must be a decimal string.");

        string text = value.GetString();
        if (text == null || !Regex.IsMatch(text, @"\A(?:0|[1-9][0-9]{0,19})\z") ||
            !ulong.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out ulong number))
            throw new InvalidDataException("An exact canonical UInt64 logical time is required.");

        return number;
    }

    public static uint UInt32Token(JsonElement value)
    {
        string text = value.GetRawText();
        if (value.ValueKind != JsonValueKind.Number || !Regex.IsMatch(text, @"\A(?:0|[1-9][0-9]{0,9})\z") ||
            !uint.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out uint number))
            throw new InvalidDataException("An exact nonnegative UInt32 JSON integer is required.");

        return number;
    }

    public static int Int32Token(JsonElement value)
    {
        string text = value.GetRawText();
        if (value.ValueKind != JsonValueKind.Number ||
            !Regex.IsMatch(text, @"\A(?:0|[1-9][0-9]{0,9}|-[1-9][0-9]{0,9})\z") ||
            !int.TryParse(text, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out int number))
            throw new InvalidDataException("An exact canonical Int32 JSON integer is required.");

        return number;
    }

    private static void ValidateBlockIdentity(JsonElement id, uint sequence, string rootHash, string fileHash)
    {
        if (id.GetProperty("@type").GetString() != "ton.blockIdExt" ||
            id.GetProperty("workchain").GetRawText() != "-1" ||
            id.GetProperty("shard").GetString() != MasterchainShard ||
            UInt32Token(id.GetProperty("seqno")) != sequence ||
            CanonicalHash(id.GetProperty("root_hash").GetString()) != rootHash ||
            CanonicalHash(id.GetProperty("file_hash").GetString()) != fileHash)
            throw new InvalidDataException("The TON masterchain block descriptor differs.");
    }

    private static JsonElement SuccessfulResult(JsonElement envelope)
    {
        RejectDuplicateProperties(envelope);
        if (envelope.ValueKind != JsonValueKind.Object ||
            envelope.GetProperty("ok").ValueKind != JsonValueKind.True ||
            envelope.GetProperty("result").ValueKind != JsonValueKind.Object)
            throw new InvalidDataException("The TON protocol did not return a successful object.");

        return envelope.GetProperty("result");
    }

    private static void RejectDuplicateProperties(JsonElement value)
    {
        if (value.ValueKind == JsonValueKind.Object)
        {
            var names = new System.Collections.Generic.HashSet<string>(StringComparer.Ordinal);
            foreach (JsonProperty property in value.EnumerateObject())
            {
                if (!names.Add(property.Name))
                    throw new InvalidDataException("Duplicate TON response property.");
                RejectDuplicateProperties(property.Value);
            }
        }
        else if (value.ValueKind == JsonValueKind.Array)
            foreach (JsonElement item in value.EnumerateArray())
                RejectDuplicateProperties(item);
    }
}
