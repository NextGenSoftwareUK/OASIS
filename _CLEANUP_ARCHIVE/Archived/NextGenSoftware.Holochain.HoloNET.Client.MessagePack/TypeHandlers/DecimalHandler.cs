

namespace NextGenSoftware.Holochain.HoloNET.Client.MessagePack
{
	public class DecimalHandler : ITypeHandler
	{
		readonly SerializationContext context;
		ITypeHandler intArrayHandler;

		public DecimalHandler(SerializationContext context)
		{
			this.context = context;
		}

		public object Read(Format format, FormatReader reader)
		{
			intArrayHandler = intArrayHandler ?? context.TypeHandlers.Get<int[]>();
			int[] bits = (int[])intArrayHandler.Read(format, reader);
			return new decimal(bits);
		}

		public void Write(object obj, FormatWriter writer)
		{
			intArrayHandler = intArrayHandler ?? context.TypeHandlers.Get<int[]>();
			int[] bits = decimal.GetBits((decimal)obj);
			intArrayHandler.Write(bits, writer);
		}
	}
}
