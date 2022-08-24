using System;
using System.IO;
using System.Text;

namespace NextGenSoftware.Holochain.HoloNET.Client.MessagePack
{
	public class FormatReader
	{
		readonly Stream stream;
		byte[] buffer = new byte[64];

		internal long Position { get { return stream.Position; } }

		public FormatReader(Stream stream)
		{
			this.stream = stream;
		}

		public Format ReadFormat()
		{
			int value = stream.ReadByte();
			if(value >= 0) {
				return new Format((byte)value);
			}
			throw new FormatException("There is nothing more to read");
		}

		public byte ReadPositiveFixInt(Format format)
		{
			return format & 0x7f;
		}

		public byte ReadUInt8()
		{
			return (byte)stream.ReadByte();
		}

		public ushort ReadUInt16()
		{
			if(stream.Read(buffer, 0, 2) == 2) {
				return (ushort)((buffer[0] << 8) | buffer[1]);
			}
			throw new FormatException();
		}

		public uint ReadUInt32()
		{
			if(stream.Read(buffer, 0, 4) == 4) {
				return ((uint)buffer[0] << 24) | ((uint)buffer[1] << 16) | ((uint)buffer[2] << 8) | (uint)buffer[3];
			}
			throw new FormatException();
		}

		public ulong ReadUInt64()
		{
			if(stream.Read(buffer, 0, 8) == 8) {
				return ((ulong)buffer[0] << 56) | ((ulong)buffer[1] << 48) | ((ulong)buffer[2] << 40) | ((ulong)buffer[3] << 32) | ((ulong)buffer[4] << 24) | ((ulong)buffer[5] << 16) | ((ulong)buffer[6] << 8) | (ulong)buffer[7];
			}
			throw new FormatException();
		}

		public sbyte ReadNegativeFixInt(Format format)
		{
			return (sbyte)((format & 0x1f) - 0x20);
		}

		public sbyte ReadInt8()
		{
			return (sbyte)stream.ReadByte();
		}

		public short ReadInt16()
		{
			if(stream.Read(buffer, 0, 2) == 2) {
				return (short)((buffer[0] << 8) | buffer[1]);
			}
			throw new FormatException();
		}

		public int ReadInt32()
		{
			if(stream.Read(buffer, 0, 4) == 4) {
				return (buffer[0] << 24) | (buffer[1] << 16) | (buffer[2] << 8) | buffer[3];
			}
			throw new FormatException();
		}

		public long ReadInt64()
		{
			if(stream.Read(buffer, 0, 8) == 8) {
				return ((long)buffer[0] << 56) | ((long)buffer[1] << 48) | ((long)buffer[2] << 40) | ((long)buffer[3] << 32) | ((long)buffer[4] << 24) | ((long)buffer[5] << 16) | ((long)buffer[6] << 8) | (long)buffer[7];
			}
			throw new FormatException();
		}

		public float ReadFloat32()
		{
			if(stream.Read(buffer, 0, 4) == 4) {
				return Float32Bits.ToSingle(buffer);
			}
			throw new FormatException();
		}

		public double ReadFloat64()
		{
			if(stream.Read(buffer, 0, 8) == 8) {
				return Float64Bits.ToDouble(buffer);
			}
			throw new FormatException();
		}

		public string ReadFixStr(Format format)
		{
			return ReadStringOfLength(format & 0x1f);
		}

		public string ReadStr8()
		{
			return ReadStringOfLength(ReadUInt8());
		}

		public string ReadStr16()
		{
			return ReadStringOfLength(ReadUInt16());
		}

		public string ReadStr32()
		{
			return ReadStringOfLength(Convert.ToInt32(ReadUInt32()));
		}

		public byte[] ReadBin8()
		{
			return ReadBytesOfLength(ReadUInt8());
		}

		public byte[] ReadBin16()
		{
			return ReadBytesOfLength(ReadUInt16());
		}

		public byte[] ReadBin32()
		{
			return ReadBytesOfLength(Convert.ToInt32(ReadUInt32()));
		}

		public int ReadArrayLength(Format format)
		{
			if(format.IsNil) return 0;
			if(format.IsFixArray) return format & 0xf;
			if(format.IsArray16) return ReadUInt16();
			if(format.IsArray32) return Convert.ToInt32(ReadUInt32());
			throw new FormatException();
		}

		public int ReadMapLength(Format format)
		{
			if(format.IsFixMap) return format & 0xf;
			if(format.IsMap16) return ReadUInt16();
			if(format.IsMap32) return Convert.ToInt32(ReadUInt32());
			throw new FormatException();
		}

		public uint ReadExtLength(Format format)
		{
			if(format.IsFixExt1) return 1;
			if(format.IsFixExt2) return 2;
			if(format.IsFixExt4) return 4;
			if(format.IsFixExt8) return 8;
			if(format.IsFixExt16) return 16;
			if(format.IsExt8) return ReadUInt8();
			if(format.IsExt16) return ReadUInt16();
			if(format.IsExt32) return ReadUInt32();
			throw new FormatException();
		}

		public sbyte ReadExtType(Format format)
		{
			if(format.IsPositiveFixInt) return (sbyte)ReadPositiveFixInt(format);
			if(format.IsUInt8) return Convert.ToSByte(ReadUInt8());
			if(format.IsNegativeFixInt) return ReadNegativeFixInt(format);
			if(format.IsInt8) return ReadInt8();
			throw new FormatException();
		}

		public void Skip()
		{
			Format format = ReadFormat();
			if(format.IsNil) { return; }
			if(format.IsFalse) { return; }
			if(format.IsTrue) { return; }
			if(format.IsPositiveFixInt) { return; }
			if(format.IsNegativeFixInt) { return; }
			if(format.IsUInt8 || format.IsInt8) { FastForward(1); return; }
			if(format.IsUInt16 || format.IsInt16) { FastForward(2); return; }
			if(format.IsUInt32 || format.IsInt32) { FastForward(4); return; }
			if(format.IsUInt64 || format.IsInt64) { FastForward(8); return; }
			if(format.IsFloat32) { FastForward(4); return; }
			if(format.IsFloat64) { FastForward(8); return; }
			if(format.IsFixStr) { FastForward(format & 0x1f); return; }
			if(format.IsStr8) { FastForward(ReadUInt8()); return; }
			if(format.IsStr16) { FastForward(ReadUInt16()); return; }
			if(format.IsStr32) { FastForward(ReadUInt32()); return; }
			if(format.IsBin8) { FastForward(ReadUInt8()); return; }
			if(format.IsBin16) { FastForward(ReadUInt16()); return; }
			if(format.IsBin32) { FastForward(ReadUInt32()); return; }
			if(format.IsArrayFamily) {
				for(int length = ReadArrayLength(format); length > 0; length--) {
					Skip();
				}
				return;
			}
			if(format.IsMapFamily) {
				for(int length = ReadMapLength(format); length > 0; length--) {
					Skip();
					Skip();
				}
				return;
			}
			if(format.IsFixExt1) { FastForward(2); return; }
			if(format.IsFixExt2) { FastForward(3); return; }
			if(format.IsFixExt4) { FastForward(5); return; }
			if(format.IsFixExt8) { FastForward(9); return; }
			if(format.IsFixExt16) { FastForward(17); return; }
			if(format.IsExt8) { FastForward(ReadUInt8() + 1); return; }
			if(format.IsExt16) { FastForward(ReadUInt16() + 1); return; }
			if(format.IsExt32) { FastForward(ReadUInt32() + 1); return; }
		}

		void FastForward(long offset)
		{
			if(stream.CanSeek) {
				stream.Seek(offset, SeekOrigin.Current);
			}
			else {
				while(offset > 0) {
					int length = offset > int.MaxValue ? int.MaxValue : (int)offset;
					ArrayHelper.AdjustSize(ref buffer, length);
					stream.Read(buffer, 0, length);
					offset -= int.MaxValue;
				}
			}
		}

		string ReadStringOfLength(int length)
		{
			ArrayHelper.AdjustSize(ref buffer, length);
			stream.Read(buffer, 0, length);
			return Encoding.UTF8.GetString(buffer, 0, length);
		}

		internal byte[] ReadBytesOfLength(int length)
		{
			byte[] bytes = new byte[length];
			stream.Read(bytes, 0, length);
			return bytes;
		}
	}
}
