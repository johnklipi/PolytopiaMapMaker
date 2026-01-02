using System.Text.Json.Serialization;

namespace PolytopiaMapManager.Data;

public class MapInfo
{
    [JsonInclude]
    public ushort size;
    [JsonInclude]
    public List<MapTile> map = new();
    [JsonInclude]
    public List<Capital> capitals = new();
    [JsonInclude]
    public List<byte> icon = new();
}