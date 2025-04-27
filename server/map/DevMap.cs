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
        // var elevation = (uint)random.Next((int)minElevation, (int)maxElevation + 1);

        // uint elevation = 1;

        uint elevation = (uint) x;

        var isForest = noise.GetNoise2D(x * 20, y * 20) > 0f;
        // var (riverInDirection, riverOutDirection) = DevStraightRiver(HexCoords.FromOffsetCoords(x, y));
        var (riverInDirection, riverOutDirection) = DevCurvyRiver(HexCoords.FromOffsetCoords(x, y));

        tiles.Add(new MapTileData {
          coords = HexCoords.FromOffsetCoords(x, y),
          elevation = elevation,
          vegetation = isForest ? VegetationType.Forest : VegetationType.Grass,
          biome = BiomeType.Grassland,
          riverInDirection = riverInDirection,
          riverOutDirection = riverOutDirection,
        });
      }
    }

    var map = new MapData(tiles);

    GD.Print("tiles generated: ", map.Tiles.Count());

    return map;
  }

  private static (HexDirection[], HexDirection[]) DevStraightRiver(HexCoords coords) {
    HexDirection[] riverInDirection = [];
    HexDirection[] riverOutDirection = [];

    if (coords.ToOffsetCoords().y == 5) {
      riverInDirection = [HexDirection.West];
      riverOutDirection = [HexDirection.East];
    }

    return (riverInDirection, riverOutDirection);
  }

  private static (HexDirection[], HexDirection[]) DevCurvyRiver(HexCoords coords) {
    HexDirection[] riverInDirection = [];
    HexDirection[] riverOutDirection = [];

    if (coords == HexCoords.FromAxialCoords(-2, 5)) {
      // riverOutDirection = [HexDirection.West];
      riverOutDirection = [HexDirection.East];
    } else if (coords == HexCoords.FromAxialCoords(-1, 5)) {
      riverInDirection = [HexDirection.West];
      // riverOutDirection = [HexDirection.NorthEast];
      riverOutDirection = [HexDirection.NorthEast, HexDirection.East];
    } else if (coords == HexCoords.FromAxialCoords(0, 4)) {
      riverInDirection = [HexDirection.SouthWest];
      riverOutDirection = [HexDirection.NorthEast];
    } else if (coords == HexCoords.FromAxialCoords(1, 3)) {
      riverInDirection = [HexDirection.SouthWest];
      riverOutDirection = [HexDirection.East];
    } else if (coords == HexCoords.FromAxialCoords(1, 3)) {
      riverInDirection = [HexDirection.SouthWest];
      riverOutDirection = [HexDirection.East];
    } else if (coords == HexCoords.FromAxialCoords(2, 3)) {
      riverInDirection = [HexDirection.West];
      riverOutDirection = [HexDirection.East];
    } else if (coords == HexCoords.FromAxialCoords(3, 3)) {
      riverInDirection = [HexDirection.West];
      riverOutDirection = [HexDirection.SouthWest];
    } else if (coords == HexCoords.FromAxialCoords(2, 4)) {
      riverInDirection = [HexDirection.NorthEast];
      riverOutDirection = [HexDirection.SouthWest];
    } else if (coords == HexCoords.FromAxialCoords(1, 5)) {
      riverInDirection = [HexDirection.NorthEast, HexDirection.West];
      // riverInDirection = [HexDirection.NorthEast];
      riverOutDirection = [HexDirection.East];
    } else if (coords == HexCoords.FromAxialCoords(2, 5)) {
      riverInDirection = [HexDirection.West];
      riverOutDirection = [HexDirection.East];
    } else if (coords == HexCoords.FromAxialCoords(3, 5)) {
      riverInDirection = [HexDirection.West];
    } else if (coords == HexCoords.FromAxialCoords(0, 5)) {
      riverInDirection = [HexDirection.West];
      riverOutDirection = [HexDirection.East];
    }

    return (riverInDirection, riverOutDirection);
  }
}