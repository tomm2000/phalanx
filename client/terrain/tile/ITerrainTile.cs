using System;
using System.Collections.Generic;
using Chickensoft.GodotNodeInterfaces;
using Godot;
using Tlib.Nodes;

public struct VertexData {
  public Vector3 position;
  public float steepness;
  public float riverness;
  public Vector2 flowDirection;
}

public interface ITerrainTile {
  public MapTileData TileData { get; }
  public IEnumerable<VertexData> Vertices { get; }
  public event Action OnTileReady;
  public event Action OnTileReadyDeferred;
  public void GenerateSurface(
    IEnumerable<(HexDirection, MapTileData)> neighbors
  );
}