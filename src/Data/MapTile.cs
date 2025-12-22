using System.Text.Json.Serialization;
using Polytopia.Data;
using PolytopiaBackendBase.Common;

namespace PolytopiaMapManager.Data;

public class MapTile
{ 
    [JsonInclude]
    public int climate = 0;
    [JsonInclude]
    [JsonConverter(typeof(PolyMod.Json.EnumCacheJson<SkinType>))]
    public SkinType skinType = SkinType.Default;
    [JsonInclude]
    [JsonConverter(typeof(PolyMod.Json.EnumCacheJson<Polytopia.Data.TerrainData.Type>))]
    public Polytopia.Data.TerrainData.Type terrain = Polytopia.Data.TerrainData.Type.Field;
    [JsonInclude]
    [JsonConverter(typeof(PolyMod.Json.EnumCacheJson<Polytopia.Data.ResourceData.Type>))]
    public Polytopia.Data.ResourceData.Type? resource;
    [JsonInclude]
    [JsonConverter(typeof(PolyMod.Json.EnumCacheJson<Polytopia.Data.ImprovementData.Type>))]
    public Polytopia.Data.ImprovementData.Type? improvement;
    [JsonInclude]
    [JsonConverter(typeof(PolyMod.Json.EnumCacheListJson<TileData.EffectType>))]
    public List<TileData.EffectType> effects = new();

    internal TileData ToTileData()
    {
        TileData tile = new TileData();

        tile.climate = (this.climate < 0 || this.climate > 16) ? 1 : this.climate;
        tile.Skin = this.skinType;
        tile.terrain = this.terrain;
        tile.resource = this.resource == null ? null : new() { type = (Polytopia.Data.ResourceData.Type)this.resource };
        tile.effects = new Il2CppSystem.Collections.Generic.List<TileData.EffectType>();
        foreach (TileData.EffectType effect in this.effects)
        {
            tile.effects.Add(effect);
        }
        switch(this.improvement)
        {
            case null:
            case ImprovementData.Type.LightHouse:
                tile.improvement = null;
                break;
            case ImprovementData.Type.City:
                tile.improvement = new ImprovementState
                {
                    type = ImprovementData.Type.City,
                    founded = 0,
                    level = 1,
                    borderSize = 1,
                    production = 1
                };
                break;
            default:
                tile.improvement = new() { type = (Polytopia.Data.ImprovementData.Type)this.improvement };
                break;
        }

        return tile;
    }
}
