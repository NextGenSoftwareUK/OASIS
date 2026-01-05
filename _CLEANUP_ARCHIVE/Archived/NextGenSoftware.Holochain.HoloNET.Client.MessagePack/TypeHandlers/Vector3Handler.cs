
//using System;
//using System.Numerics;

//namespace NextGenSoftware.Holochain.HoloNET.Client.MessagePack
//{
//	public class Vector3Handler : ITypeHandler
//	{
//		readonly SerializationContext context;
//		ITypeHandler floatHandler;

//		public Vector3Handler(SerializationContext context)
//		{
//			this.context = context;
//		}

//		public object Read(Format format, FormatReader reader)
//		{
//			if(format.IsArrayFamily) {
//				floatHandler = floatHandler ?? context.TypeHandlers.Get<float>();
//				Vector3 vector = new Vector3();
//				vector.x = (float)floatHandler.Read(reader.ReadFormat(), reader);
//				vector.y = (float)floatHandler.Read(reader.ReadFormat(), reader);
//				vector.z = (float)floatHandler.Read(reader.ReadFormat(), reader);
//				return vector;
//			}
//			throw new FormatException(this, format, reader);
//		}

//		public void Write(object obj, FormatWriter writer)
//		{
//			Vector3 vector = (Vector3)obj;
//			writer.WriteArrayHeader(3);
//			writer.Write(vector.x);
//			writer.Write(vector.y);
//			writer.Write(vector.z);
//		}
//	}
//}
