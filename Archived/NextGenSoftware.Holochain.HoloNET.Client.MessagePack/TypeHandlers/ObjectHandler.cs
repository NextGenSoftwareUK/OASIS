using System.Collections.Generic;


namespace NextGenSoftware.Holochain.HoloNET.Client.MessagePack
{
	public class ObjectHandler : ITypeHandler
	{
		readonly SerializationContext context;

		public ObjectHandler(SerializationContext context)
		{
			this.context = context;
		}

		public object Read(Format format, FormatReader reader)
		{
			if(format.IsNil) return null;
			if(format.IsFalse) return false;
			if(format.IsTrue) return true;
			if(format.IsPositiveFixInt) return reader.ReadPositiveFixInt(format);
			if(format.IsUInt8) return reader.ReadUInt8();
			if(format.IsUInt16) return reader.ReadUInt16();
			if(format.IsUInt32) return reader.ReadUInt32();
			if(format.IsUInt64) return reader.ReadUInt64();
			if(format.IsNegativeFixInt) return reader.ReadNegativeFixInt(format);
			if(format.IsInt8) return reader.ReadInt8();
			if(format.IsInt16) return reader.ReadInt16();
			if(format.IsInt32) return reader.ReadInt32();
			if(format.IsInt64) return reader.ReadInt64();
			if(format.IsFloat32) return reader.ReadFloat32();
			if(format.IsFloat64) return reader.ReadFloat64();
			if(format.IsFixStr) return reader.ReadFixStr(format);
			if(format.IsStr8) return reader.ReadStr8();
			if(format.IsStr16) return reader.ReadStr16();
			if(format.IsStr32) return reader.ReadStr32();
			if(format.IsBin8) return reader.ReadBin8();
			if(format.IsBin16) return reader.ReadBin16();
			if(format.IsBin32) return reader.ReadBin32();
			if(format.IsArrayFamily) return ReadArray(format, reader);
			if(format.IsMapFamily) return ReadMap(format, reader);
			if(format.IsExtFamily) return ReadExt(format, reader);
			throw new FormatException(this, format, reader);
		}

		public void Write(object obj, FormatWriter writer)
		{
			if(obj == null) {
				writer.WriteNil();
				return;
			}
			context.TypeHandlers.Get(obj.GetType()).Write(obj, writer);
		}

		object ReadArray(Format format, FormatReader reader)
		{
			return context.TypeHandlers.Get<List<object>>().Read(format, reader);
		}

		object ReadMap(Format format, FormatReader reader)
		{
			return context.TypeHandlers.Get<Dictionary<object, object>>().Read(format, reader);
		}

		object ReadExt(Format format, FormatReader reader)
		{
			uint length = reader.ReadExtLength(format);
			sbyte extType = reader.ReadExtType(format);
			return context.TypeHandlers.GetExt(extType).ReadExt(length, reader);
		}
	}
}
