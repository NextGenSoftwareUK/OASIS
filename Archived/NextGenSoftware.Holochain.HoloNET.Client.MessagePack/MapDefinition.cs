using System;
using System.Collections.Generic;
using System.Drawing;
using System.Numerics;
using System.Reflection;

namespace NextGenSoftware.Holochain.HoloNET.Client.MessagePack
{
	public class MapDefinition
	{
		const BindingFlags MethodFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.InvokeMethod;

		static readonly Type[] serializableUnityTypes = {
			typeof(Color),
			typeof(Vector2), typeof(Vector3), typeof(Vector4),
			typeof(Quaternion),
			#if UNITY_2017_2_OR_NEWER
			typeof(Vector2Int), typeof(Vector3Int),
			#endif
		};

		static readonly Type[] callbackTypes = {
			typeof(OnDeserializingAttribute), typeof(OnDeserializedAttribute),
			typeof(OnSerializingAttribute), typeof(OnSerializedAttribute),
		};

		public readonly Type Type;
		public readonly Dictionary<string, FieldInfo> FieldInfos;
		public readonly Dictionary<string, ITypeHandler> FieldHandlers;
		public readonly Dictionary<Type, MethodInfo[]> Callbacks;

		internal MapDefinition(SerializationContext context, Type type)
		{
			this.Type = type;

			if(!IsSerializable(context, type))
			{
				throw new CustomAttributeFormatException(type + " does not have System.SerializableAttribute defined");
			}

			this.FieldInfos = new Dictionary<string, FieldInfo>();
			foreach(FieldInfo info in type.GetFields(context.MapOptions.FieldFlags)) {
				if(IsFieldSerializable(context, info)) {
					FieldInfos[info.Name] = info;
				}
			}

			this.FieldHandlers = new Dictionary<string, ITypeHandler>();
			foreach(FieldInfo info in FieldInfos.Values) {
				FieldHandlers.Add(info.Name, context.TypeHandlers.Get(info.FieldType));
			}

			this.Callbacks = new Dictionary<Type, MethodInfo[]>();
			MethodInfo[] methodInfos = type.GetMethods(MethodFlags);
			foreach(Type callbackType in callbackTypes) {
				List<MethodInfo> methodsWithCallbacks = new List<MethodInfo>();
				foreach(MethodInfo methodInfo in methodInfos) {
					if(AttributesExist(methodInfo, callbackType)) {
						methodsWithCallbacks.Add(methodInfo);
					}
				}
				if(methodsWithCallbacks.Count > 0) {
					Callbacks[callbackType] = methodsWithCallbacks.ToArray();
				}
			}
		}

		bool IsSerializable(SerializationContext context, Type type)
		{
			if(!context.MapOptions.RequireSerializableAttribute) {
				return true;
			}

			if(Array.IndexOf(serializableUnityTypes, type) != -1) {
				return true;
			}

			return type.IsSerializable;
		}

		bool AttributesExist(MemberInfo info, Type attributeType)
		{
			return info.GetCustomAttributes(attributeType, true).Length > 0;
		}

		bool IsFieldSerializable(SerializationContext context, FieldInfo info)
		{
			if(AttributesExist(info, typeof(NonSerializedAttribute))) {
				return false;
			}
			if(context.MapOptions.IgnoreAutoPropertyValues && info.Name.StartsWith("<")) {
				return false;
			}
			return IsSerializable(context, info.FieldType);
		}
	}
}
