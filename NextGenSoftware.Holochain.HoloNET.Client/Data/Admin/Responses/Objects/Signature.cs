
using MessagePack;
using System;

namespace NextGenSoftware.Holochain.HoloNET.Client
{
    [MessagePackObject]
    public struct Signature
    {
       // [Key("Value")]
        public byte[] Value { get; private set; }

        public Signature(byte[] value)
        {
            if (value.Length != 64)
                throw new ArgumentException("Signature must be 64 bytes long.", nameof(value));

            Value = value;
        }
    }
}