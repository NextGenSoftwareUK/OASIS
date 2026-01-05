using System;
using System.Runtime.InteropServices;

namespace NextGenSoftware.Holochain.HoloNET.Client.MessagePack
{
	// Taken and modified from Yusuke FUJIWARA's "MessagePack for CLI"
	// https://github.com/msgpack/msgpack-cli/blob/master/src/MsgPack/Float64Bits.cs
	[StructLayout(LayoutKind.Explicit)]
	struct Float64Bits
	{
		[FieldOffset(0)] double value;
		[FieldOffset(0)] byte byte0;
		[FieldOffset(1)] byte byte1;
		[FieldOffset(2)] byte byte2;
		[FieldOffset(3)] byte byte3;
		[FieldOffset(4)] byte byte4;
		[FieldOffset(5)] byte byte5;
		[FieldOffset(6)] byte byte6;
		[FieldOffset(7)] byte byte7;

		Float64Bits(double value)
		{
			this = default(Float64Bits);
			this.value = value;
		}

		internal static void GetBytes(double value, byte[] buffer)
		{
			var bits = new Float64Bits(value);
			if(BitConverter.IsLittleEndian) {
				buffer[0] = bits.byte7;
				buffer[1] = bits.byte6;
				buffer[2] = bits.byte5;
				buffer[3] = bits.byte4;
				buffer[4] = bits.byte3;
				buffer[5] = bits.byte2;
				buffer[6] = bits.byte1;
				buffer[7] = bits.byte0;
			}
			else {
				buffer[0] = bits.byte0;
				buffer[1] = bits.byte1;
				buffer[2] = bits.byte2;
				buffer[3] = bits.byte3;
				buffer[4] = bits.byte4;
				buffer[5] = bits.byte5;
				buffer[6] = bits.byte6;
				buffer[7] = bits.byte7;
			}
		}

		internal static double ToDouble(byte[] bigEndianBytes)
		{
			Float64Bits bits = default(Float64Bits);
			if(BitConverter.IsLittleEndian) {
				bits.byte0 = bigEndianBytes[7];
				bits.byte1 = bigEndianBytes[6];
				bits.byte2 = bigEndianBytes[5];
				bits.byte3 = bigEndianBytes[4];
				bits.byte4 = bigEndianBytes[3];
				bits.byte5 = bigEndianBytes[2];
				bits.byte6 = bigEndianBytes[1];
				bits.byte7 = bigEndianBytes[0];
			}
			else {
				bits.byte0 = bigEndianBytes[0];
				bits.byte1 = bigEndianBytes[1];
				bits.byte2 = bigEndianBytes[2];
				bits.byte3 = bigEndianBytes[3];
				bits.byte4 = bigEndianBytes[4];
				bits.byte5 = bigEndianBytes[5];
				bits.byte6 = bigEndianBytes[6];
				bits.byte7 = bigEndianBytes[7];
			}
			return bits.value;
		}
	}
}