#if UNITY_2017_2_OR_NEWER


namespace NextGenSoftware.Holochain.HoloNET.Client.MessagePack
{
	public class Vector2IntHandler : ITypeHandler
	{
		readonly SerializationContext context;
		ITypeHandler intHandler;

		public Vector2IntHandler(SerializationContext context)
		{
			this.context = context;
		}

		public object Read(Format format, FormatReader reader)
		{
			if(format.IsArrayFamily) {
				intHandler = intHandler ?? context.TypeHandlers.Get<int>();
				Vector2Int vector = new Vector2Int();
				vector.x = (int)intHandler.Read(reader.ReadFormat(), reader);
				vector.y = (int)intHandler.Read(reader.ReadFormat(), reader);
				return vector;
			}
			throw new FormatException(this, format, reader);
		}

		public void Write(object obj, FormatWriter writer)
		{
			Vector2Int vector = (Vector2Int)obj;
			writer.WriteArrayHeader(2);
			writer.Write(vector.x);
			writer.Write(vector.y);
		}
	}
}
#endif
