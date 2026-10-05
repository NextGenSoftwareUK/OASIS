
namespace NextGenSoftware.Holochain.HoloNET.Client.MessagePack
{
	public struct Format
	{
		readonly public byte Value;

		public Format(byte value)
		{
			this.Value = value;
		}

		public const byte PositiveFixIntMin = 0x00;
		public const byte PositiveFixIntMax = 0x7f;
		public const byte FixMapMin = 0x80;
		public const byte FixMapMax = 0x8f;
		public const byte FixArrayMin = 0x90;
		public const byte FixArrayMax = 0x9f;
		public const byte FixStrMin = 0xa0;
		public const byte FixStrMax = 0xbf;
		public const byte Nil = 0xc0;
		public const byte NeverUsed = 0xc1;
		public const byte False = 0xc2;
		public const byte True = 0xc3;
		public const byte Bin8 = 0xc4;
		public const byte Bin16 = 0xc5;
		public const byte Bin32 = 0xc6;
		public const byte Ext8 = 0xc7;
		public const byte Ext16 = 0xc8;
		public const byte Ext32 = 0xc9;
		public const byte Float32 = 0xca;
		public const byte Float64 = 0xcb;
		public const byte UInt8 = 0xcc;
		public const byte UInt16 = 0xcd;
		public const byte UInt32 = 0xce;
		public const byte UInt64 = 0xcf;
		public const byte Int8 = 0xd0;
		public const byte Int16 = 0xd1;
		public const byte Int32 = 0xd2;
		public const byte Int64 = 0xd3;
		public const byte FixExt1 = 0xd4;
		public const byte FixExt2 = 0xd5;
		public const byte FixExt4 = 0xd6;
		public const byte FixExt8 = 0xd7;
		public const byte FixExt16 = 0xd8;
		public const byte Str8 = 0xd9;
		public const byte Str16 = 0xda;
		public const byte Str32 = 0xdb;
		public const byte Array16 = 0xdc;
		public const byte Array32 = 0xdd;
		public const byte Map16 = 0xde;
		public const byte Map32 = 0xdf;
		public const byte NegativeFixIntMin = 0xe0;
		public const byte NegativeFixIntMax = 0xff;

		public bool IsPositiveFixInt { get { return Between(PositiveFixIntMin, PositiveFixIntMax); } }
		public bool IsFixMap { get { return Between(FixMapMin, FixMapMax); } }
		public bool IsFixArray { get { return Between(FixArrayMin, FixArrayMax); } }
		public bool IsFixStr { get { return Between(FixStrMin, FixStrMax); } }
		public bool IsNil { get { return Value == Nil; } }
		public bool IsNeverUsed { get { return Value == NeverUsed; } }
		public bool IsFalse { get { return Value == False; } }
		public bool IsTrue { get { return Value == True; } }
		public bool IsBin8 { get { return Value == Bin8; } }
		public bool IsBin16 { get { return Value == Bin16; } }
		public bool IsBin32 { get { return Value == Bin32; } }
		public bool IsExt8 { get { return Value == Ext8; } }
		public bool IsExt16 { get { return Value == Ext16; } }
		public bool IsExt32 { get { return Value == Ext32; } }
		public bool IsFloat32 { get { return Value == Float32; } }
		public bool IsFloat64 { get { return Value == Float64; } }
		public bool IsUInt8 { get { return Value == UInt8; } }
		public bool IsUInt16 { get { return Value == UInt16; } }
		public bool IsUInt32 { get { return Value == UInt32; } }
		public bool IsUInt64 { get { return Value == UInt64; } }
		public bool IsInt8 { get { return Value == Int8; } }
		public bool IsInt16 { get { return Value == Int16; } }
		public bool IsInt32 { get { return Value == Int32; } }
		public bool IsInt64 { get { return Value == Int64; } }
		public bool IsFixExt1 { get { return Value == FixExt1; } }
		public bool IsFixExt2 { get { return Value == FixExt2; } }
		public bool IsFixExt4 { get { return Value == FixExt4; } }
		public bool IsFixExt8 { get { return Value == FixExt8; } }
		public bool IsFixExt16 { get { return Value == FixExt16; } }
		public bool IsStr8 { get { return Value == Str8; } }
		public bool IsStr16 { get { return Value == Str16; } }
		public bool IsStr32 { get { return Value == Str32; } }
		public bool IsArray16 { get { return Value == Array16; } }
		public bool IsArray32 { get { return Value == Array32; } }
		public bool IsMap16 { get { return Value == Map16; } }
		public bool IsMap32 { get { return Value == Map32; } }
		public bool IsNegativeFixInt { get { return Between(NegativeFixIntMin, NegativeFixIntMax); } }
		public bool IsEmptyArray { get { return Value == FixArrayMin; } }

		public bool IsIntFamily { get { return IsPositiveFixInt || IsNegativeFixInt || IsInt8 || IsUInt8 || IsInt16 || IsUInt16 || IsInt32 || IsUInt32 || IsInt64 || IsUInt64; } }
		public bool IsBoolFamily { get { return IsFalse || IsTrue; } }
		public bool IsFloatFamily { get { return IsFloat32 || IsFloat64; } }
		public bool IsStringFamily { get { return IsFixStr || IsStr8 || IsStr16 || IsStr32; } }
		public bool IsBinaryFamily { get { return IsBin8 || IsBin16 || IsBin32; } }
		public bool IsArrayFamily { get { return IsFixArray || IsArray16 || IsArray32; } }
		public bool IsMapFamily { get { return IsFixMap || IsMap16 || IsMap32; } }
		public bool IsExtFamily { get { return IsFixExt1 || IsFixExt2 || IsFixExt4 || IsFixExt8 || IsFixExt16 || IsExt8 || IsExt16 || IsExt32; } }

		bool Between(byte min, byte max)
		{
			return Value >= min && Value <= max;
		}

		public override int GetHashCode()
		{
			return this.Value.GetHashCode();
		}

		public override bool Equals(object obj)
		{
			if(obj is Format) {
				return this.Value == ((Format)obj).Value;
			}
			if(obj is byte) {
				return this.Value == (byte)obj;
			}
			return false;
		}

		public static byte operator &(Format f1, byte value)
		{
			return (byte)(f1.Value & value);
		}

		public static bool operator ==(Format f1, Format f2)
		{
			return f1.Value == f2.Value;
		}

		public static bool operator !=(Format f1, Format f2)
		{
			return f1.Value != f2.Value;
		}

		public override string ToString()
		{
			return string.Concat("0x", Value.ToString("X2"));
		}
	}
}