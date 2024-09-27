using MessagePack;
using System;
using System.Runtime.InteropServices;

namespace NextGenSoftware.Holochain.HoloNET.Client
{
   // [MessagePackObject]
    public struct ZomeCallUnsigned
    {
        //[Key("provenance")]
        public byte[] provenance;

        //[Key("cell_id_dna_hash")]
        public byte[] cell_id_dna_hash;

        //[Key("cell_id_agent_pub_key")]
        public byte[] cell_id_agent_pub_key;

        //[Key("zome_name")]
        public string zome_name;

        //[Key("fn_name")]
        public string fn_name;

        //[Key("cap_secret")]
        public byte[] cap_secret;

        //[Key("payload")]
        public byte[] payload;

        //[Key("nonce")]
        public byte[] nonce;

        //[Key("expires_at")]
        public Int64 expires_at;
    }

    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct ZomeCallUnsignedRaw
    {
        public byte* provenance;
        public byte* cell_id_dna_hash;
        public byte* cell_id_agent_pub_key;
        public string zome_name;
        public string fn_name;
        public byte* cap_secret;
        public byte* payload;
        public UInt32 payload_length;
        public byte* nonce;
        public Int64 expires_at;
        
    }

    public class HolochainSerialisationWrapper
    {
        [DllImport("holochain_serialisation_wrapper.dll", CallingConvention = CallingConvention.Cdecl)]
        internal static extern void get_data_to_sign([In, Out] byte[] data, ZomeCallUnsignedRaw zome_call_unsigned);

        internal static void call_get_data_to_sign(byte[] data, ZomeCallUnsigned zome_call_unsigned)
        {
            unsafe
            {
                fixed (byte* provenance = zome_call_unsigned.provenance, cell_id_dna_hash = zome_call_unsigned.cell_id_dna_hash, cell_id_agent_pub_key = zome_call_unsigned.cell_id_agent_pub_key, cap_secret = zome_call_unsigned.cap_secret, payload = zome_call_unsigned.payload, nonce = zome_call_unsigned.nonce)
                {
                    var raw_call = new ZomeCallUnsignedRaw
                    {
                        provenance = provenance,
                        cell_id_dna_hash = cell_id_dna_hash,
                        cell_id_agent_pub_key = cell_id_agent_pub_key,
                        zome_name = zome_call_unsigned.zome_name,
                        fn_name = zome_call_unsigned.fn_name,
                        cap_secret = cap_secret,
                        payload = payload,
                        payload_length = (UInt32)zome_call_unsigned.payload.Length,
                        nonce = nonce,
                        expires_at = zome_call_unsigned.expires_at,
                    };

                    get_data_to_sign(data, raw_call);
                }
            }
        }
    }
}
