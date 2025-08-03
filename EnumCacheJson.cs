using System.Text.Json;
using System.Text.Json.Serialization;

namespace PolytopiaMapManager
{
	internal class EnumCacheJson<T> : JsonConverter<T> where T : struct, Enum
	{
		public override bool CanConvert(Type typeToConvert)
		{
			return typeToConvert == typeof(T) || typeToConvert == typeof(List<T>);
		}

		public override T Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
		{
			if (typeToConvert == typeof(T))
			{
				return EnumCache<T>.GetType(reader.GetString());
			}

			throw new NotSupportedException("EnumCacheJson does not support reading this type directly.");
		}

		public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
		{
			writer.WriteStringValue(EnumCache<T>.GetName(value));
		}

		public override T ReadAsPropertyName(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
		{
			return Read(ref reader, typeToConvert, options);
		}

		public override void WriteAsPropertyName(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
		{
			writer.WritePropertyName(EnumCache<T>.GetName(value));
		}
	}

	internal class EnumCacheListJson<T> : JsonConverter<List<T>> where T : struct, Enum
	{
		private readonly EnumCacheJson<T> _inner = new();

		public override List<T> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
		{
			var list = new List<T>();

			if (reader.TokenType != JsonTokenType.StartArray)
				throw new JsonException("Expected StartArray token for enum list");

			while (reader.Read())
			{
				if (reader.TokenType == JsonTokenType.EndArray)
					break;

				list.Add(_inner.Read(ref reader, typeof(T), options));
			}

			return list;
		}

		public override void Write(Utf8JsonWriter writer, List<T> value, JsonSerializerOptions options)
		{
			writer.WriteStartArray();

			foreach (var item in value)
			{
				_inner.Write(writer, item, options);
			}

			writer.WriteEndArray();
		}
	}
}
