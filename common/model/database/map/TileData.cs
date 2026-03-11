using System;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using Godot;
using MessagePack;
using Tlib.Hex;

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

  public JsonObject ToJson() {
    var json = new JsonObject {
      ["elevation"] = elevation,
      ["vegetation"] = vegetation.ToString(),
      ["biome"] = biome.ToString(),
      ["riverInDirection"] = new JsonArray([.. riverInDirection.Select(dir => dir.ToString())]),
      ["riverOutDirection"] = new JsonArray([.. riverOutDirection.Select(dir => dir.ToString())]),
    };

    return json;
  }

  public static MapTileData FromJson(HexCoords coords, JsonElement json) {
    return new MapTileData {
      coords = coords,
      elevation = json.GetProperty("elevation").GetUInt32(),
      vegetation = Enum.Parse<VegetationType>(json.GetProperty("vegetation").GetString()!),
      biome = Enum.Parse<BiomeType>(json.GetProperty("biome").GetString()!),
      riverInDirection = json.GetProperty("riverInDirection").EnumerateArray().Select(e => Enum.Parse<HexDirection>(e.GetString()!)).ToArray(),
      riverOutDirection = json.GetProperty("riverOutDirection").EnumerateArray().Select(e => Enum.Parse<HexDirection>(e.GetString()!)).ToArray(),
    };
  }
}