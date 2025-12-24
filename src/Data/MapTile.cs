using System.Text.Json.Serialization;
using Polytopia.Data;
using PolytopiaBackendBase.Common;

namespace PolytopiaMapManager.Data;

public class MapTile
{
    [JsonInclude]
    public int climate = DEFAULT_CLIMATE;
    [JsonInclude]
    [JsonConverter(typeof(PolyMod.Json.EnumCacheJson<SkinType>))]
    public SkinType skinType = DEFAULT_SKIN;
    [JsonInclude]
    [JsonConverter(typeof(PolyMod.Json.EnumCacheJson<TerrainData.Type>))]
    public TerrainData.Type terrain = TerrainData.Type.Field;
    [JsonInclude]
    [JsonConverter(typeof(PolyMod.Json.EnumCacheJson<ResourceData.Type>))]
    public ResourceData.Type? resource;
    [JsonInclude]
    [JsonConverter(typeof(PolyMod.Json.EnumCacheJson<ImprovementData.Type>))]
    public ImprovementData.Type? improvement;
    [JsonInclude]
    [JsonConverter(typeof(PolyMod.Json.EnumCacheListJson<TileData.EffectType>))]
    public List<TileData.EffectType> effects = new();
    const int DEFAULT_CLIMATE = 2;
    const SkinType DEFAULT_SKIN = SkinType.Default;

    internal TileData ToTileData()
    {
        TileData tile = new TileData();
        tile.climate = IsClimateValid(climate) ? climate : DEFAULT_CLIMATE;
        tile.Skin = IsSkinTypeValid(skinType) ? skinType : DEFAULT_SKIN;
        tile.terrain = terrain;
        tile.resource = resource == null ? null : new() { type = (Polytopia.Data.ResourceData.Type)resource };
        tile.effects = new Il2CppSystem.Collections.Generic.List<TileData.EffectType>();
        foreach (TileData.EffectType effect in effects)
        {
            tile.effects.Add(effect);
        }
        switch(improvement)
        {
            case ImprovementData.Type.City:
                tile.improvement = new()
                {
                    type = ImprovementData.Type.City,
                    founded = 0,
                    level = 1,
                    borderSize = 1,
                    production = 1
                };
                break;
            case ImprovementData.Type.Ruin:
                tile.improvement = new()
                {
                    type = (ImprovementData.Type)improvement
                };
                break;
            default:
                tile.improvement = null;
                break;
        }

        return tile;
    }

    public bool IsClimateValid(int climate)
    {
        var maxLength = Enum.GetValues(typeof(TribeType)).Length - 2;
        return climate >= 0 && climate <= maxLength;
    }

    public bool IsSkinTypeValid(SkinType skin)
    {
        var maxLength = Enum.GetValues(typeof(SkinType)).Cast<SkinType>().ToList();
        maxLength.Remove(SkinType.Test);
        return maxLength.Contains(skin);
    }
}
