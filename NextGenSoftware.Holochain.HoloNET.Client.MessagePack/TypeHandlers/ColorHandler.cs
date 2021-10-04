
//using System.Collections.Generic;
//using System.Drawing;

//namespace NextGenSoftware.Holochain.HoloNET.Client.MessagePack
//{
//	public class ColorHandler : ITypeHandler
//	{
//		readonly SerializationContext context;
//		ITypeHandler floatHandler;
//		ITypeHandler stringHandler;
//		ITypeHandler mapHandler;

//		public ColorHandler(SerializationContext context)
//		{
//			this.context = context;
//		}

//		public object Read(Format format, FormatReader reader)
//		{
//			if(format.IsArrayFamily) {
//				floatHandler = floatHandler ?? context.TypeHandlers.Get<float>();
//				int length = reader.ReadArrayLength(format);
//				float[] bytes = new float[length];
//				for(int i = 0; i < length; i++) {
//					bytes[i] = (float)floatHandler.Read(reader.ReadFormat(), reader);
//				}
//				return new Color(bytes[0], bytes[1], bytes[2], bytes[3]);
//			}
//			if(format.IsStringFamily) {
//				stringHandler = stringHandler ?? context.TypeHandlers.Get<string>();
//				Color color;
//				ColorUtility.TryParseHtmlString((string)stringHandler.Read(format, reader), out color);
//				return color;
//			}
//			if(format.IsMapFamily) {
//				mapHandler = mapHandler ?? context.TypeHandlers.Get<Dictionary<string, float>>();
//				Dictionary<string, float> map = (Dictionary<string, float>)mapHandler.Read(format, reader);
//				return new Color(map["r"], map["g"], map["b"], map["a"]);
//			}
//			throw new FormatException(this, format, reader);
//		}

//		public void Write(object obj, FormatWriter writer)
//		{
//			Color color = (Color)obj;
//			writer.WriteArrayHeader(4);
//			writer.Write(color.r);
//			writer.Write(color.g);
//			writer.Write(color.b);
//			writer.Write(color.a);
//		}
//	}
//}
