using System;
using System.Collections;
using System.Collections.Generic;


namespace NextGenSoftware.Holochain.HoloNET.Client.MessagePack
{
	public class DynamicListHandler : ITypeHandler
	{
		readonly SerializationContext context;
		readonly Type innerType;
		readonly ITypeHandler innerTypeHandler;

		public DynamicListHandler(SerializationContext context, Type type)
		{
			this.context = context;
			this.innerType = type.GetGenericArguments()[0];
			this.innerTypeHandler = context.TypeHandlers.Get(innerType);
		}

		public object Read(Format format, FormatReader reader)
		{
			Type listType = typeof(List<>).MakeGenericType(new[] { innerType });

			if(format.IsArrayFamily) {
				IList list = (IList)Activator.CreateInstance(listType);
				int size = reader.ReadArrayLength(format);
				for(int i = 0; i < size; i++) {
					list.Add(innerTypeHandler.Read(reader.ReadFormat(), reader));
				}
				return list;
			}
			if(format.IsNil) {
				if(context.ArrayOptions.NullAsEmptyOnUnpack) {
					return Activator.CreateInstance(listType);
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
			IList values = (IList)obj;
			writer.WriteArrayHeader(values.Count);
			foreach(object value in values) {
				innerTypeHandler.Write(value, writer);
			}
		}
	}
}
