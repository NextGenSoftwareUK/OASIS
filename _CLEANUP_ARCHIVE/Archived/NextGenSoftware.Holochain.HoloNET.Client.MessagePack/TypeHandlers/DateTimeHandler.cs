using System;


namespace NextGenSoftware.Holochain.HoloNET.Client.MessagePack
{
	public class DateTimeHandler : IExtTypeHandler
	{
		readonly static DateTime epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

		readonly SerializationContext context;
		ITypeHandler stringHandler;
		ITypeHandler doubleHandler;

		public sbyte ExtType { get { return -1; } }

		public DateTimeHandler(SerializationContext context)
		{
			this.context = context;
		}

		public object Read(Format format, FormatReader reader)
		{
			if(format.IsExtFamily) {
				uint length = reader.ReadExtLength(format);
				if(ExtType == reader.ReadExtType(reader.ReadFormat())) {
					return ReadExt(length, reader);
				}
			}
			if(format.IsStringFamily) {
				stringHandler = stringHandler ?? context.TypeHandlers.Get<string>();
				string dateTimeStr = (string)stringHandler.Read(format, reader);
				return DateTime.Parse(dateTimeStr);
			}
			if(format.IsFloatFamily || format.IsIntFamily) {
				doubleHandler = doubleHandler ?? context.TypeHandlers.Get<double>();
				double seconds = (double)doubleHandler.Read(format, reader);
				return epoch.AddSeconds(seconds).ToLocalTime();
			}
			throw new FormatException(this, format, reader);
		}

		public object ReadExt(uint length, FormatReader reader)
		{
			// Timestamp 32
			if(length == 4) {
				return epoch.AddSeconds(reader.ReadUInt32()).ToLocalTime();
			}
			// Timestamp 64
			if(length == 8) {
				byte[] buffer = reader.ReadBytesOfLength(8);
				uint nanoseconds = ((uint)buffer[0] << 22) | ((uint)buffer[1] << 14) | ((uint)buffer[2] << 6) | (uint)buffer[3] >> 2;
				ulong seconds = ((ulong)(buffer[3] & 0x3) << 32) | ((ulong)buffer[4] << 24) | ((ulong)buffer[5] << 16) | ((ulong)buffer[6] << 8) | (ulong)buffer[7];
				return epoch.AddTicks(nanoseconds / 100).AddSeconds(seconds).ToLocalTime();
			}
			// Timestamp 96
			if(length == 12) {
				return epoch.AddTicks(reader.ReadUInt32() / 100).AddSeconds(reader.ReadInt64()).ToLocalTime();
			}
			throw new FormatException();
		}

		public void Write(object obj, FormatWriter writer)
		{
			DateTime value = (DateTime)obj;
			switch(context.DateTimeOptions.PackingFormat) {
				case DateTimePackingFormat.Extension:
					TimeSpan diff = value.ToUniversalTime() - epoch;
					writer.WriteExtHeader(12, ExtType);
					writer.WriteUInt32((uint)(value.Ticks % 10000000) * 100);
					writer.WriteUInt64((ulong)diff.TotalSeconds);
					break;
				case DateTimePackingFormat.String:
					writer.Write(value.ToString("o"));
					break;
				case DateTimePackingFormat.Epoch:
					writer.Write((value.ToUniversalTime() - epoch).TotalSeconds);
					break;
				default:
					throw new FormatException();
			}
		}
	}
}
