using Polytopia.Data;
using PolytopiaBackendBase.Common;

namespace PolytopiaMapManager;
public class ChangeTileCommand : CommandBase
{
    public TribeType Tribe { get; set; }

    public SkinType Skin { get; set; }
	public ImprovementData.Type Improvement { get; set; }

    public ResourceData.Type Resource { get; set; }

    public TerrainData.Type Terrain { get; set; }

    public TileData.EffectType TileEffect { get; set; }

	public WorldCoordinates Coordinates { get; set; }

	public ChangeTileCommand(IntPtr ptr) : base(ptr) {}
	public ChangeTileCommand() {}

	public ChangeTileCommand(byte playerId, TribeType tribe, SkinType skin,
        ImprovementData.Type improvement, ResourceData.Type resource,
        TerrainData.Type terrain, TileData.EffectType tileEffect, WorldCoordinates coordinates)
		: base(playerId)
	{
		this.Tribe = tribe;
        this.Skin = skin;
        this.Improvement = improvement;
        this.Resource = resource;
        this.Terrain = terrain;
        this.TileEffect = tileEffect;
		this.Coordinates = coordinates;
	}

	public override bool IsValid(GameState state, out string validationError)
	{
        validationError = "";
		return true;
	}

	public override CommandType GetCommandType()
	{
		CommandType type = EnumCache<CommandType>.GetType("changetilecommand");
		return type;
	}

	public void ExecuteNew(GameState state)
	{
        Console.Write("THIS SHIT IS EXECUTING");
		TileData tileData = state.Map.GetTile(this.Coordinates);
		tileData.Skin = Skin;
		tileData.terrain = Terrain;

	}

	public override bool ShouldAskForConfirmation()
	{
		return false;
	}

	public void SerializeNew(Il2CppSystem.IO.BinaryWriter writer, int version)
	{
		writer.Write((ushort)this.Tribe);
        writer.Write((ushort)this.Skin);
        writer.Write((ushort)this.Improvement);
        writer.Write((ushort)this.Resource);
        writer.Write((ushort)this.Terrain);
        writer.Write((ushort)this.TileEffect);
		this.Coordinates.Serialize(writer, version);
	}

	public void DeserializeNew(Il2CppSystem.IO.BinaryReader reader, int version)
	{
		this.Tribe = (TribeType)reader.ReadUInt16();
        this.Skin = (SkinType)reader.ReadUInt16();
        this.Improvement = (ImprovementData.Type)reader.ReadUInt16();
        this.Resource = (ResourceData.Type)reader.ReadUInt16();
        this.Terrain = (TerrainData.Type)reader.ReadUInt16();
        this.TileEffect = (TileData.EffectType)reader.ReadUInt16();
		this.Coordinates = new WorldCoordinates(reader, version);
	}

	public override string ToString()
	{
		return string.Format("{0} (PlayerId: {1}, Tribe: {2}, Skin: {3}, Improvement: {4}, Resource: {5}, Terrain: {6}, TileEffect: {7}, Coordinates: {8})", new object[]
		{
			base.GetType(),
			base.PlayerId,
			this.Tribe,
            this.Skin,
            this.Improvement,
            this.Resource,
            this.Terrain,
            this.TileEffect,
			this.Coordinates
		});
	}
}