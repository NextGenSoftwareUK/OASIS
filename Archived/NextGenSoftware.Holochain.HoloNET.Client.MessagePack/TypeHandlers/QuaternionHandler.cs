

//using System.Numerics;

//namespace NextGenSoftware.Holochain.HoloNET.Client.MessagePack
//{
//	public class QuaternionHandler : ITypeHandler
//	{
//		readonly SerializationContext context;
//		ITypeHandler floatHandler;

//		public QuaternionHandler(SerializationContext context)
//		{
//			this.context = context;
//		}

//		public object Read(Format format, FormatReader reader)
//		{
//			if(format.IsArrayFamily) {
//				floatHandler = floatHandler ?? context.TypeHandlers.Get<float>();
//				Quaternion quaternion = new Quaternion();
//				quaternion.x = (float)floatHandler.Read(reader.ReadFormat(), reader);
//				quaternion.y = (float)floatHandler.Read(reader.ReadFormat(), reader);
//				quaternion.z = (float)floatHandler.Read(reader.ReadFormat(), reader);
//				quaternion.w = (float)floatHandler.Read(reader.ReadFormat(), reader);
//				return quaternion;
//			}
//			throw new FormatException(this, format, reader);
//		}

//		public void Write(object obj, FormatWriter writer)
//		{
//			Quaternion quaternion = (Quaternion)obj;
//			writer.WriteArrayHeader(4);
//			writer.Write(quaternion.x);
//			writer.Write(quaternion.y);
//			writer.Write(quaternion.z);
//			writer.Write(quaternion.w);
//		}
//	}
//}
