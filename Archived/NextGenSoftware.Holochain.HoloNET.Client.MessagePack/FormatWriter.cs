using System.IO;
using System.Text;

namespace NextGenSoftware.Holochain.HoloNET.Client.MessagePack
{
	public class FormatWriter
	{
		readonly Stream stream;
		byte[] buffer = new byte[64];

		public FormatWriter(Stream stream)
		{
			this.stream = stream;
		}

		public void WriteFormat(byte formatValue)
		{
			stream.WriteByte(formatValue);
		}

		public void WriteNil()
		{
			stream.WriteByte(Format.Nil);
		}

		public void Write(bool value)
		{
			stream.WriteByte(value ? Format.True : Format.False);
		}

		public void Write(byte value)
		{
			if(value <= sbyte.MaxValue) {
				WritePositiveFixInt(value);
			}
			else {
				WriteFormat(Format.UInt8);
				WriteUInt8(value);
			}
		}

		public void Write(ushort value)
		{
			if(value <= byte.MaxValue) {
				Write((byte)value);
			}
			else {
				WriteFormat(Format.UInt16);
				WriteUInt16(value);
			}
		}

		public void Write(uint value)
		{
			if(value <= ushort.MaxValue) {
				Write((ushort)value);
			}
			else {
				WriteFormat(Format.UInt32);
				WriteUInt32(value);
			}
		}

		public void Write(ulong value)
		{
			if(value <= uint.MaxValue) {
				Write((uint)value);
			}
			else {
				WriteFormat(Format.UInt64);
				WriteUInt64(value);
			}
		}

		public void Write(sbyte value)
		{
			if(value >= 0) {
				Write((byte)value);
			}
			else if(value >= -32) {
				WriteNegativeFixInt(value);
			}
			else {
				WriteFormat(Format.Int8);
				WriteInt8(value);
			}
		}

		public void Write(short value)
		{
			if(value >= 0) {
				Write((ushort)value);
			}
			else if(value >= sbyte.MinValue) {
				Write((sbyte)value);
			}
			else {
				WriteFormat(Format.Int16);
				WriteInt16(value);
			}
		}

		public void Write(int value)
		{
			if(value >= 0) {
				Write((uint)value);
			}
			else if(value >= short.MinValue) {
				Write((short)value);
			}
			else {
				WriteFormat(Format.Int32);
				WriteInt32(value);
			}
		}

		public void Write(long value)
		{
			if(value >= 0) {
				Write((ulong)value);
			}
			else if(value >= int.MinValue) {
				Write((int)value);
			}
			else {
				WriteFormat(Format.Int64);
				WriteInt64(value);
			}
		}

		public void Write(float value)
		{
			WriteFormat(Format.Float32);
			Float32Bits.GetBytes(value, buffer);
			stream.Write(buffer, 0, 4);
		}

		public void Write(double value)
		{
			WriteFormat(Format.Float64);
			Float64Bits.GetBytes(value, buffer);
			stream.Write(buffer, 0, 8);
		}

		public void Write(string value)
		{
			if(value == null) {
				WriteNil();
				return;
			}

			int length = Encoding.UTF8.GetByteCount(value);

			if(length <= 31) {
				WriteFormat((byte)(Format.FixStrMin | (byte)length));
			}
			else if(length <= byte.MaxValue) {
				WriteFormat(Format.Str8);
				WriteUInt8((byte)length);
			}
			else if(length <= ushort.MaxValue) {
				WriteFormat(Format.Str16);
				WriteUInt16((ushort)length);
			}
			else {
				WriteFormat(Format.Str32);
				WriteUInt32((uint)length);
			}

			ArrayHelper.AdjustSize(ref buffer, length);
			Encoding.UTF8.GetBytes(value, 0, value.Length, buffer, 0);
			stream.Write(buffer, 0, length);
		}

		public void Write(byte[] bytes)
		{
			if(bytes == null) {
				WriteNil();
				return;
			}

			if(bytes.Length <= byte.MaxValue) {
				WriteFormat(Format.Bin8);
				WriteUInt8((byte)bytes.Length);
			}
			else if(bytes.Length <= ushort.MaxValue) {
				WriteFormat(Format.Bin16);
				WriteUInt16((ushort)bytes.Length);
			}
			else {
				WriteFormat(Format.Bin32);
				WriteUInt32((uint)bytes.Length);
			}
			stream.Write(bytes, 0, bytes.Length);
		}

		public void WriteArrayHeader(int length)
		{
			if(length <= 15) {
				WriteFormat((byte)(length | Format.FixArrayMin));
			}
			else if(length <= ushort.MaxValue) {
				WriteFormat(Format.Array16);
				WriteUInt16((ushort)length);
			}
			else {
				WriteFormat(Format.Array32);
				WriteUInt32((uint)length);
			}
		}

		public void WriteBinHeader(int length)
		{
			if(length <= byte.MaxValue) {
				WriteFormat(Format.Bin8);
				WriteUInt8((byte)length);
			}
			else if(length <= ushort.MaxValue) {
				WriteFormat(Format.Bin16);
				WriteUInt16((ushort)length);
			}
			else {
				WriteFormat(Format.Bin32);
				WriteUInt32((uint)length);
			}
		}

		public void WriteMapHeader(int length)
		{
			if(length <= 15) {
				WriteFormat((byte)(length | Format.FixMapMin));
			}
			else if(length <= ushort.MaxValue) {
				WriteFormat(Format.Map16);
				WriteUInt16((ushort)length);
			}
			else {
				WriteFormat(Format.Map32);
				WriteUInt32((uint)length);
			}
		}

		public void WriteExtHeader(uint length, sbyte extType)
		{
			if(length == 1) {
				WriteFormat(Format.FixExt1);
			}
			else if(length == 2) {
				WriteFormat(Format.FixExt2);
			}
			else if(length == 4) {
				WriteFormat(Format.FixExt4);
			}
			else if(length == 8) {
				WriteFormat(Format.FixExt8);
			}
			else if(length == 16) {
				WriteFormat(Format.FixExt16);
			}
			else if(length <= byte.MaxValue) {
				WriteFormat(Format.Ext8);
				WriteUInt8((byte)length);
			}
			else if(length <= ushort.MaxValue) {
				WriteFormat(Format.Ext16);
				WriteUInt16((ushort)length);
			}
			else if(length <= uint.MaxValue) {
				WriteFormat(Format.Ext32);
				WriteUInt32(length);
			}
			else {
				throw new FormatException();
			}
			stream.WriteByte((byte)extType);
		}

		public void WritePositiveFixInt(byte value)
		{
			if(value >= 0 || value <= sbyte.MaxValue) {
				stream.WriteByte((byte)(value | Format.PositiveFixIntMin));
			}
			else {
				throw new FormatException(value + " is out of range for PositiveFixInt");
			}
		}

		public void WriteUInt8(byte value)
		{
			stream.WriteByte(value);
		}

		public void WriteUInt16(ushort value)
		{
			buffer[0] = (byte)(value >> 8);
			buffer[1] = (byte)(value);
			stream.Write(buffer, 0, 2);
		}

		public void WriteUInt32(uint value)
		{
			buffer[0] = (byte)(value >> 24);
			buffer[1] = (byte)(value >> 16);
			buffer[2] = (byte)(value >> 8);
			buffer[3] = (byte)(value);
			stream.Write(buffer, 0, 4);
		}

		public void WriteUInt64(ulong value)
		{
			buffer[0] = (byte)(value >> 56);
			buffer[1] = (byte)(value >> 48);
			buffer[2] = (byte)(value >> 40);
			buffer[3] = (byte)(value >> 32);
			buffer[4] = (byte)(value >> 24);
			buffer[5] = (byte)(value >> 16);
			buffer[6] = (byte)(value >> 8);
			buffer[7] = (byte)(value);
			stream.Write(buffer, 0, 8);
		}

		public void WriteNegativeFixInt(sbyte value)
		{
			if(value >= -32 && value <= -1) {
				stream.WriteByte((byte)((byte)value | Format.NegativeFixIntMin));
			}
			else {
				throw new FormatException(value + " is out of range for NegativeFixInt");
			}
		}

		public void WriteInt8(sbyte value)
		{
			stream.WriteByte((byte)value);
		}

		public void WriteInt16(short value)
		{
			buffer[0] = (byte)(value >> 8);
			buffer[1] = (byte)(value);
			stream.Write(buffer, 0, 2);
		}

		public void WriteInt32(int value)
		{
			buffer[0] = (byte)(value >> 24);
			buffer[1] = (byte)(value >> 16);
			buffer[2] = (byte)(value >> 8);
			buffer[3] = (byte)(value);
			stream.Write(buffer, 0, 4);
		}

		public void WriteInt64(long value)
		{
			buffer[0] = (byte)(value >> 56);
			buffer[1] = (byte)(value >> 48);
			buffer[2] = (byte)(value >> 40);
			buffer[3] = (byte)(value >> 32);
			buffer[4] = (byte)(value >> 24);
			buffer[5] = (byte)(value >> 16);
			buffer[6] = (byte)(value >> 8);
			buffer[7] = (byte)(value);
			stream.Write(buffer, 0, 8);
		}

		public void WriteRawByte(byte value)
		{
			stream.WriteByte(value);
		}
	}
}
