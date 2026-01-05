using System;


namespace NextGenSoftware.Holochain.HoloNET.Client.MessagePack
{
	public class DynamicArrayHandler : ITypeHandler
	{
		readonly SerializationContext context;
		readonly Type elementType;
		readonly ITypeHandler elementTypeHandler;

		public DynamicArrayHandler(SerializationContext context, Type type)
		{
			this.context = context;
			this.elementType = type.GetElementType();
			this.elementTypeHandler = context.TypeHandlers.Get(elementType);
		}

		public object Read(Format format, FormatReader reader)
		{
			if(format.IsArrayFamily) {
				int size = reader.ReadArrayLength(format);
				Array array = Array.CreateInstance(elementType, size);
				for(int i = 0; i < size; i++) {
					object value = elementTypeHandler.Read(reader.ReadFormat(), reader);
					array.SetValue(value, i);
				}
				return array;
			}
			if(format.IsNil) {
				if(context.ArrayOptions.NullAsEmptyOnUnpack) {
					return Array.CreateInstance(elementType, 0);
				}
				return null;
			}
			throw new FormatException(this, format, reader);
		}

		public void Write(object obj, FormatWriter writer)
		{
			if(obj == null) {
				writer.WriteNil();
				return;
			}
			Array values = (Array)obj;
			writer.WriteArrayHeader(values.Length);
			foreach(object value in values) {
				elementTypeHandler.Write(value, writer);
			}
		}
	}
}
