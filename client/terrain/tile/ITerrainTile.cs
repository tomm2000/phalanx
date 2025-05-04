using System;
using System.Collections.Generic;
using Chickensoft.GodotNodeInterfaces;
using Godot;
using Tlib.Nodes;

public struct VertexData {
  public Vector3 position;
  public HexVertexIndex index;
  public float steepness;
  public Vector2 UV;
  public float riverFactor;
  public Vector2 riverFlowDirection;

  public static implicit operator Vector3(VertexData vertex) => vertex.position;

}

public static class VertexDataExtensions {
  public static VertexData With(
    this VertexData vertexFrom,
    Vector3? position = null,
    HexVertexIndex? index = null,
    float? steepness = null,
    Vector2? UV = null,
    float? riverFactor = null,
    Vector2? riverFlowDirection = null
  ) {
    return new VertexData {
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
  public IEnumerable<VertexData> Vertices { get; }
  public event Action OnTileReady;
  public void GenerateSurface(
    IEnumerable<(HexDirection, MapTileData)> neighbors
  );
  public void SetShader(TerrainShader shader);
}