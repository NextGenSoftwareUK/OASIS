using System;


namespace NextGenSoftware.Holochain.HoloNET.Client.MessagePack
{
	public class DynamicEnumHandler : ITypeHandler
	{
		readonly SerializationContext context;
		readonly Type type;
		ITypeHandler intHandler;
		ITypeHandler stringHandler;

		public DynamicEnumHandler(SerializationContext context, Type type)
		{
			this.context = context;
			this.type = type;
		}

		public object Read(Format format, FormatReader reader)
		{
			if(format.IsIntFamily) {
				intHandler = intHandler ?? context.TypeHandlers.Get<int>();
				return Enum.ToObject(type, intHandler.Read(format, reader));
			}
			if(format.IsStringFamily) {
				stringHandler = stringHandler ?? context.TypeHandlers.Get<string>();
				return Enum.Parse(type, (string)stringHandler.Read(format, reader), true);
			}
			if(format.IsNil) {
				return Enum.ToObject(type, 0);
			}
			throw new FormatException(this, format, reader);
		}

		public void Write(object obj, FormatWriter writer)
		{
			switch(context.EnumOptions.PackingFormat) {
				case EnumPackingFormat.Integer:
              		intHandler = intHandler ?? context.TypeHandlers.Get<int>();
					intHandler.Write(obj, writer);
					break;
				case EnumPackingFormat.String:
					stringHandler = stringHandler ?? context.TypeHandlers.Get<string>();
					stringHandler.Write(obj.ToString(), writer);
					break;
			}
		}
	}
}
