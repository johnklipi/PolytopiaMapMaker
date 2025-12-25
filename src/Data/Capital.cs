using System.Text.Json.Serialization;

namespace PolytopiaMapManager.Data;

public class Capital
{
    [JsonInclude]
    public byte player;
    [JsonInclude]
    public WorldCoordinates coordinates;
}