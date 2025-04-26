using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

public static class DevMap {
  public static MapData GenerateMap(
    int width = 2,
    int height = 2,
    uint maxElevation = 4,
    uint minElevation = 1,
    int? seed = null
  ) {
    var tiles = new List<MapTileData>();

    var random = seed == null ? new Random() : new Random(seed.Value);

    var noiseTexture = new NoiseTexture2D {
      Noise = new FastNoiseLite()
    };
    var noise = noiseTexture.Noise;

    for (int y = 0; y < height; y++) {
      for (int x = 0; x < width; x++) {
        var elevation = (uint)random.Next((int)minElevation, (int)maxElevation + 1);

        // uint elevation = 1;

        // uint elevation = (uint) x;

        var isForest = noise.GetNoise2D(x * 20, y * 20) > 0f;

        tiles.Add(new MapTileData {
          coords = HexCoords.FromOffsetCoords(x, y),
          elevation = elevation,
          vegetation = isForest ? VegetationType.Forest : VegetationType.Grass,
          biome = BiomeType.Grassland,
        });
      }
    }

    var map = new MapData(tiles);

    GD.Print("tiles generated: ", map.Tiles.Count());

    return map;
  }
}