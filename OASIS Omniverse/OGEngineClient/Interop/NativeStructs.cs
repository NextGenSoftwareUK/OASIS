using System.Runtime.InteropServices;

namespace NextGenSoftware.OASIS.STARAPI.Client;

[StructLayout(LayoutKind.Sequential, Pack = 8)]
public unsafe struct ogengine_config_t
{
    public sbyte* base_url;
    public sbyte* api_key;
    public sbyte* avatar_id;
    public int timeout_seconds;
    /// <summary>Optional: runtime client id for cross-game quest UI (e.g. ODOOM, OQUAKE). Null = infer from objective/quest/last progress only.</summary>
    public sbyte* client_game_source;
    /// <summary>0 = <see cref="OGEngineTransport.Remote"/>, 1 = <see cref="OGEngineTransport.Native"/> (requires native host; default ogengine returns InitFailed).</summary>
    public int transport;
    /// <summary>Optional UTF-8 path to OASIS_DNA.json for native transport.</summary>
    public sbyte* oasis_dna_path;
}

[StructLayout(LayoutKind.Sequential)]
public unsafe struct ogengine_item_t
{
    public fixed byte id[64];
    public fixed byte name[256];
    public fixed byte description[512];
    public fixed byte game_source[64];
    public fixed byte item_type[64];
    public fixed byte nft_id[128];
    public int quantity;
}

[StructLayout(LayoutKind.Sequential)]
public unsafe struct ogengine_item_list_t
{
    public ogengine_item_t* items;
    public nuint count;
    public nuint capacity;
}

/// <summary>C layout for ogengine_sync_local_item_t (name, description, game_source, item_type, nft_id, synced). Used by ogengine_sync_inventory_start.</summary>
[StructLayout(LayoutKind.Sequential)]
public unsafe struct ogengine_sync_local_item_t
{
    public fixed byte name[256];
    public fixed byte description[512];
    public fixed byte game_source[64];
    public fixed byte item_type[64];
    public fixed byte nft_id[128];
    public int synced;
}

