using System.Text.Json.Serialization;

namespace PolytopiaMapManager.Data;

public class MapInfo
{
    [JsonInclude]
    public ushort size;
    [JsonInclude]
    public List<MapTile> map = new();
}