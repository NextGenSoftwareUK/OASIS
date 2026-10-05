using System;


namespace NextGenSoftware.Holochain.HoloNET.Client.MessagePack
{
	public class DynamicNullableHandler : ITypeHandler
	{
		readonly ITypeHandler underlyingTypeHandler;

		public DynamicNullableHandler(SerializationContext context, Type type)
		{
			Type underlyingType = Nullable.GetUnderlyingType(type);
			this.underlyingTypeHandler = context.TypeHandlers.Get(underlyingType);
		}

		public object Read(Format format, FormatReader reader)
		{
			if(format.IsNil) return null;
			return underlyingTypeHandler.Read(format, reader);
		}

		public void Write(object obj, FormatWriter writer)
		{
			if(obj == null) {
				writer.WriteNil();
				return;
			}
			underlyingTypeHandler.Write(obj, writer);
		}
	}
}
