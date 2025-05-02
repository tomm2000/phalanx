using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Chickensoft.AutoInject;
using Chickensoft.Introspection;
using Client.Terrain;
using Godot;
using Tlib.Nodes;


[Meta(typeof(IAutoConnect), typeof(IAutoNode))]
public partial class IndexedTile : Node3D, ITerrainTile {
  public override void _Notification(int what) => this.Notify(what);
  public static readonly string ScenePath = "uid://bmydftsu36dtn";

  #region public interface
  public MapTileData TileData { get; private set; } = default!;
  public IEnumerable<VertexData> Vertices => throw new NotImplementedException();
  public event Action? OnTileReady;
  public event Action? OnTileReadyDeferred;
  #endregion

  #region private properties
  private SurfaceTool surfaceTool = new();
  #endregion

  public static IndexedTile Instantiate(MapTileData tileData) {
    var scene = GD.Load<PackedScene>(ScenePath);
    var instance = scene.Instantiate<IndexedTile>();
    instance.TileData = tileData;

    return instance;
  }

  #region Nodes
  [Node] MeshInstance3D MeshInstance { get; set; } = default!;
  [Dependency] StandardTerrain terrain => this.DependOn<StandardTerrain>();
  #endregion

  public void OnResolved() {
  }

  public void GenerateSurface(
    IEnumerable<(HexDirection, MapTileData)> neighbors
  ) {
    var strips = 4;
    surfaceTool.Clear();
    surfaceTool.Begin(Mesh.PrimitiveType.Triangles);

    // var worldPosition = TileData.coords.GridToWorld3D();
    // var vertices = TlibHexMesher.GenerateVertices(strips, 1);

    // var filterIndices = TlibHexMesher.GetHexAnchorIndices(1);
    // var filterIndices = TlibHexMesher.GetHexEdgeIndices(3);
    // vertices = vertices.Where(v => filterIndices.Contains(v.Key))
    //   .ToDictionary(v => v.Key, v => v.Value);

    // create a ball mesh at each vertex
    // foreach (var (index, vertex) in vertices) {
    //   // GD.Print(vertex);
    //   var sphere = DebugSphere.Instantiate();
    //   sphere.Position = vertex;
    //   AddChild(sphere);
    // }

    var triangles = TlibHexMesher.GenerateTriangles(strips, 1);
    var expenctedTriangles = strips*strips*6;

    if (triangles.Count() != expenctedTriangles) {
      GD.PrintErr($"Expected {expenctedTriangles} triangles, got {triangles.Count()}");
    }

    foreach (var triangle in triangles) {
      var a = triangle.A;
      var b = triangle.B;
      var c = triangle.C;

      surfaceTool.AddVertex(a);
      surfaceTool.AddVertex(b);
      surfaceTool.AddVertex(c);
    }

    var mesh = surfaceTool.Commit();

    MeshInstance.Mesh = mesh;
  }
}
