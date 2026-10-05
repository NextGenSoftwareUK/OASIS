using System;
using System.Collections.Generic;
using System.Reflection;


namespace NextGenSoftware.Holochain.HoloNET.Client.MessagePack
{
	public class DynamicMapHandler : ITypeHandler
	{
		readonly SerializationContext context;
		readonly Lazy<MapDefinition> lazyDefinition;
		readonly ITypeHandler nameHandler;
		readonly IMapNamingStrategy nameConverter;
		readonly static object[] callbackParameters = new object[0];

		public DynamicMapHandler(SerializationContext context, Lazy<MapDefinition> lazyDefinition)
		{
			this.context = context;
			this.lazyDefinition = lazyDefinition;
			this.nameHandler = context.TypeHandlers.Get<string>();
			this.nameConverter = context.MapOptions.NamingStrategy;
		}

		public object Read(Format format, FormatReader reader)
		{
			MapDefinition definition = lazyDefinition.Value;
			if(format.IsMapFamily) {
				object obj = Activator.CreateInstance(definition.Type);
				InvokeCallback<OnDeserializingAttribute>(obj, definition);
				int size = reader.ReadMapLength(format);
				while(size > 0) {
					string name = (string)nameHandler.Read(reader.ReadFormat(), reader);
					name = nameConverter.OnUnpack(name, definition);

					if(definition.FieldHandlers.ContainsKey(name)) {
						object value = definition.FieldHandlers[name].Read(reader.ReadFormat(), reader);
						definition.FieldInfos[name].SetValue(obj, value);
					}
					else if(context.MapOptions.IgnoreUnknownFieldOnUnpack) {
						reader.Skip();
					}
					else {
						throw new MissingFieldException(name + " does not exist for type: " + definition.Type);
					}
					size = size - 1;
				}
				InvokeCallback<OnDeserializedAttribute>(obj, definition);
				return obj;
			}
			if(format.IsEmptyArray && context.MapOptions.AllowEmptyArrayOnUnpack) {
				return Activator.CreateInstance(definition.Type);
			}
			if(format.IsNil) {
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
			MapDefinition definition = lazyDefinition.Value;
			InvokeCallback<OnSerializingAttribute>(obj, definition);
			writer.WriteMapHeader(DetermineSize(obj, definition));
			foreach(KeyValuePair<string, FieldInfo> kv in definition.FieldInfos) {
				object value = kv.Value.GetValue(obj);
				if(context.MapOptions.IgnoreNullOnPack && value == null) {
					continue;
				}
				string name = nameConverter.OnPack(kv.Key, definition);
				nameHandler.Write(name, writer);
				definition.FieldHandlers[kv.Key].Write(value, writer);
			}
			InvokeCallback<OnSerializedAttribute>(obj, definition);
		}

		int DetermineSize(object obj, MapDefinition definition)
		{
			if(!context.MapOptions.IgnoreNullOnPack) {
				return definition.FieldInfos.Count;
			}
			int count = 0;
			foreach(FieldInfo info in definition.FieldInfos.Values) {
				if(info.GetValue(obj) != null) {
					count += 1;
				}
			}
			return count;
		}

		void InvokeCallback<T>(object obj, MapDefinition definition) where T : Attribute
		{
			Type attributeType = typeof(T);
			if(definition.Callbacks.ContainsKey(attributeType)) {
				foreach(MethodInfo methodInfo in definition.Callbacks[attributeType]) {
					methodInfo.Invoke(obj, callbackParameters);
				}
			}
		}
	}
}
