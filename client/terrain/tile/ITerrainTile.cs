using System;
using System.Collections.Generic;
using Chickensoft.GodotNodeInterfaces;
using Godot;
using Tlib.Hex;

public struct TerrainVertexData: IVertexData {
  public Vector3 position { get; set; }
  public HexVertexIndex index { get; set; }
  public float steepness;
  public Vector2 UV;
  public float riverFactor;
  public Vector2 riverFlowDirection;
}

public static class VertexDataExtensions {
  public static TerrainVertexData With(
    this TerrainVertexData vertexFrom,
    Vector3? position = null,
    HexVertexIndex? index = null,
    float? steepness = null,
    Vector2? UV = null,
    float? riverFactor = null,
    Vector2? riverFlowDirection = null
  ) {
    return new TerrainVertexData {
      position = position ?? vertexFrom.position,
      index = index ?? vertexFrom.index,
      steepness = steepness ?? vertexFrom.steepness,
      UV = UV ?? vertexFrom.UV,
      riverFactor = riverFactor ?? vertexFrom.riverFactor,
      riverFlowDirection = riverFlowDirection ?? vertexFrom.riverFlowDirection
    };
  }
}

public interface ITerrainTile {
  public MapTileData TileData { get; }
  public IEnumerable<TerrainVertexData> Vertices { get; }
  public event Action OnTileReady;
  public void GenerateSurface(
    IEnumerable<(HexDirection, MapTileData)> neighbors
  );
  public void SetShader(TerrainShader shader);
}