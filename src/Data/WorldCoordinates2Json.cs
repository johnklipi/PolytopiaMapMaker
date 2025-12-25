using System.Text.Json;
using System.Text.Json.Serialization;

namespace PolytopiaMapManager.Data;

/// <summary>
/// Converts a <see cref="WorldCoordinates"/> to and from a JSON array of two numbers.
/// </summary>
public class WorldCoordinates2Json : JsonConverter<WorldCoordinates>
{
    /// <summary>
    /// Reads and converts the JSON to a <see cref="WorldCoordinates"/>.
    /// </summary>
    /// <param name="reader">The reader.</param>
    /// <param name="typeToConvert">The type to convert.</param>
    /// <param name="options">An object that specifies serialization options to use.</param>
    /// <returns>The converted value.</returns>
    public override WorldCoordinates Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        List<int> values = new();
        if (reader.TokenType == JsonTokenType.StartArray)
        {
            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndArray) break;
                if (reader.TokenType != JsonTokenType.Number) throw new JsonException();
                values.Add(reader.GetInt32());
            }
        }
        if (values.Count != 2) throw new JsonException();
        return new(values[0], values[1]);
    }

    /// <summary>
    /// Writes a specified value as JSON.
    /// </summary>
    /// <param name="writer">The writer to write to.</param>
    /// <param name="value">The value to convert to JSON.</param>
    /// <param name="options">An object that specifies serialization options to use.</param>
    public override void Write(Utf8JsonWriter writer, WorldCoordinates value, JsonSerializerOptions options)
    {
        writer.WriteStartArray();
        writer.WriteNumberValue(value.x);
        writer.WriteNumberValue(value.y);
        writer.WriteEndArray();
    }
}