using System;


namespace NextGenSoftware.Holochain.HoloNET.Client.MessagePack
{
	public class GuidHandler : ITypeHandler
	{
		readonly SerializationContext context;
		ITypeHandler binaryHandler;
		ITypeHandler stringHandler;

		public GuidHandler(SerializationContext context)
		{
			this.context = context;
		}

		public object Read(Format format, FormatReader reader)
		{
			if(format.IsBin8) {
				binaryHandler = binaryHandler ?? context.TypeHandlers.Get<byte[]>();
				return new Guid((byte[])binaryHandler.Read(format, reader));
			}
			if(format.IsStr8) {
				stringHandler = stringHandler ?? context.TypeHandlers.Get<string>();
				return new Guid((string)stringHandler.Read(format, reader));				
			}
			throw new FormatException(this, format, reader);
		}

		public void Write(object obj, FormatWriter writer)
		{
			binaryHandler = binaryHandler ?? context.TypeHandlers.Get<byte[]>();
			binaryHandler.Write(((Guid)obj).ToByteArray(), writer);
		}
	}
}
