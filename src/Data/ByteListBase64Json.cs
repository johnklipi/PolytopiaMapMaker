using System.Text.Json;
using System.Text.Json.Serialization;

namespace PolytopiaMapManager.Data;

public class ByteListBase64Json : JsonConverter<List<byte>>
{
    public override List<byte> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return ReadBytes(ref reader).ToList();
    }

    public override void Write(Utf8JsonWriter writer, List<byte> value, JsonSerializerOptions options)
    {
        if (value.Count == 0)
        {
            writer.WriteStartArray();
            writer.WriteEndArray();
            return;
        }
        writer.WriteBase64StringValue(value.ToArray());
    }

    internal static byte[] ReadBytes(ref Utf8JsonReader reader)
    {
        if (reader.TokenType == JsonTokenType.String)
            return reader.GetBytesFromBase64();

        if (reader.TokenType != JsonTokenType.StartArray)
            throw new JsonException("Expected a base64 string or a byte array.");

        List<byte> bytes = new();
        while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
            bytes.Add(reader.GetByte());
        return bytes.ToArray();
    }
}
