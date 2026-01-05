using System;
using System.Runtime.InteropServices;

namespace NextGenSoftware.Holochain.HoloNET.Client.MessagePack
{
	// Taken and modified from Yusuke FUJIWARA's "MessagePack for CLI"
	// https://github.com/msgpack/msgpack-cli/blob/master/src/MsgPack/Float32Bits.cs
	[StructLayout(LayoutKind.Explicit)]
	struct Float32Bits
	{
		[FieldOffset(0)] float value;
		[FieldOffset(0)] byte byte0;
		[FieldOffset(1)] byte byte1;
		[FieldOffset(2)] byte byte2;
		[FieldOffset(3)] byte byte3;

		Float32Bits(float value)
		{
			this = default(Float32Bits);
			this.value = value;
		}

		internal static void GetBytes(float value, byte[] buffer)
		{
			Float32Bits bits = new Float32Bits(value);
			if(BitConverter.IsLittleEndian) {
				buffer[0] = bits.byte3;
				buffer[1] = bits.byte2;
				buffer[2] = bits.byte1;
				buffer[3] = bits.byte0;
			}
			else {
				buffer[0] = bits.byte0;
				buffer[1] = bits.byte1;
				buffer[2] = bits.byte2;
				buffer[3] = bits.byte3;
			}
		}

		internal static float ToSingle(byte[] bigEndianBytes)
		{
			Float32Bits bits = default(Float32Bits);
			if(BitConverter.IsLittleEndian) {
				bits.byte0 = bigEndianBytes[3];
				bits.byte1 = bigEndianBytes[2];
				bits.byte2 = bigEndianBytes[1];
				bits.byte3 = bigEndianBytes[0];
			}
			else {
				bits.byte0 = bigEndianBytes[0];
				bits.byte1 = bigEndianBytes[1];
				bits.byte2 = bigEndianBytes[2];
				bits.byte3 = bigEndianBytes[3];
			}
			return bits.value;
		}
	}
}