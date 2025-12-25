using System.Text.Json.Serialization;
using UnityEngine;

namespace PolytopiaMapManager.Data;

public class MapInfo
{
    [JsonInclude]
    public ushort size;
    [JsonInclude]
    public List<MapTile> map = new();
    [JsonInclude]
    public List<Capital> capitals = new();
}