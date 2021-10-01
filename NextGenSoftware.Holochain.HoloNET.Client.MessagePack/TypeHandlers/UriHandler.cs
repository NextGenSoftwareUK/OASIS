using System;


namespace NextGenSoftware.Holochain.HoloNET.Client.MessagePack
{
	public class UriHandler : ITypeHandler
	{
		readonly SerializationContext context;
		ITypeHandler stringHandler;

		public UriHandler(SerializationContext context)
		{
			this.context = context;
		}

		ITypeHandler GetStringHandler()
		{
			return stringHandler = stringHandler ?? context.TypeHandlers.Get<string>();
		}

		public object Read(Format format, FormatReader reader)
		{
			if(format.IsNil) return null;
			return new Uri((string)GetStringHandler().Read(format, reader));
		}

		public void Write(object obj, FormatWriter writer)
		{
			if(obj == null) {
				writer.WriteNil();
				return;
			}
			string value = ((Uri)obj).ToString();
			GetStringHandler().Write(value, writer);
		}
	}
}
