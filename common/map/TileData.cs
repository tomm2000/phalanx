using System.Text.Json;
using Godot;
using MessagePack;

public enum VegetationType {
  None,
  Grass,
  Forest,
  Shrub,
}

public enum BiomeType {
  Grassland,
  Forest,
}

[MessagePackObject]
public readonly struct MapTileData {
  [Key(0)] public readonly HexCoords coords { init; get; }
  [Key(1)] public readonly uint elevation { init; get; }
  [Key(2)] public readonly VegetationType vegetation { init; get; }
  [Key(3)] public readonly BiomeType biome { init; get; }

  [Key(4)] public readonly HexDirection[] riverInDirection { init; get; }
  [Key(5)] public readonly HexDirection[] riverOutDirection { init; get; }
  [IgnoreMember] public readonly bool isRiver => riverInDirection.Length > 0 || riverOutDirection.Length > 0;

  public override string ToString() {
    var river = riverInDirection == null ? "None" : $"{string.Join(", ", riverInDirection)} -> {string.Join(", ", riverOutDirection)}";

    return $"""
    TileData
    - Elevation: {elevation}
    - Coords: {coords}
    - Vegetation: {vegetation}
    - Biome: {biome}
    - River: {river}
    """;
  }
}