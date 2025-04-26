using System;
using System.Threading.Tasks;
using Godot;
using Chickensoft.AutoInject;
using Chickensoft.Introspection;
using Tlib.Nodes;
using System.Collections.Generic;
using Tlib;
using System.Linq;

namespace Client.Terrain;

public struct VertexData {
  public Vector3 position;
  public float steepness;
}

[Meta(typeof(IAutoConnect), typeof(IAutoNode))]
public partial class StandardTile : Node3D {
  public override void _Notification(int what) => this.Notify(what);
  public static readonly string ScenePath = "uid://clkgqxom5swiy";

  [Node] private MeshInstance3D MeshInstance { get; set; } = default!;
  [Node] private CollisionShape3D CollisionShape { get; set; } = default!;
  [Node] private Area3D CollisionArea { get; set; } = default!;

  [Export] private NoiseTexture2D TerrainNoise { get; set; } = default!;

  private SurfaceTool surfaceTool = new();
  private HashSet<VertexData> vertices = [];
  public IEnumerable<VertexData> Vertices => vertices;

  public MapTileData TileData { get; private set; } = default!;
  public Action OnTileReady { get; set; } = default!;
  public Action OnTileReadyDeferred { get; set; } = default!;

  public static StandardTile Instantiate(MapTileData tileData) {
    var scene = GD.Load<PackedScene>(ScenePath);
    var instance = scene.Instantiate<StandardTile>();
    instance.TileData = tileData;

    return instance;
  }

  public override void _Ready() {
    CollisionArea.InputEvent += OnInputEvent;
  }

  public void SetShader(TerrainShader shader) {
    var loadedShader = TerrainShaders.GetShader(shader);
    var shaderMaterial = new ShaderMaterial();
    shaderMaterial.Shader = loadedShader;
    MeshInstance.MaterialOverride = shaderMaterial;
  }

  #region Surface Interaction
  public void GenerateInputMesh() {
    var shape = MeshInstance.Mesh.CreateTrimeshShape();

    shape.SetFaces(shape.GetFaces());

    CollisionShape.SetDeferred("shape", shape);
  }

  private void OnInputEvent(Node camera, InputEvent @event, Vector3 position, Vector3 normal, long shapeIdx) {
    if (@event is InputEventMouseButton mouseButton) {
      if (mouseButton.Pressed) {
        ClientEventBus.TileClicked(TileData, mouseButton.ButtonIndex);
      }
    }

    if (@event is InputEventMouseMotion mouseMotion) {
      ClientEventBus.TileHovered(TileData);
    }

  }
  #endregion

  #region Surface Generation
  public void GenerateSurface(
    IEnumerable<(HexDirection, MapTileData)> neighbors,
    float terrainScale = 1f
  ) {
    var worldPosition = TileData.coords.GridToWorld3D(terrainScale);

    SetDeferred("position", worldPosition);

    surfaceTool.Clear();
    surfaceTool.Begin(Godot.Mesh.PrimitiveType.Triangles);

    var triangles = Meshes.CreateHexagonMesh(radius: terrainScale, Constants.TERRAIN_MESH_SUBDIVISIONS);

    foreach (var triangle in triangles) {
      var a = ProjectVertex(triangle.A, TileData, terrainScale, neighbors);
      var b = ProjectVertex(triangle.B, TileData, terrainScale, neighbors);
      var c = ProjectVertex(triangle.C, TileData, terrainScale, neighbors);

      vertices.Add(a);
      vertices.Add(b);
      vertices.Add(c);

      var a_vector = a.position;
      var b_vector = b.position;
      var c_vector = c.position;

      var uv_a = VertexToUV(a_vector);
      var uv_b = VertexToUV(b_vector);
      var uv_c = VertexToUV(c_vector);

      var triangleNormal = (a_vector - b_vector).Cross(c_vector - a_vector).Normalized();

      float normalDot = 1 - Math.Abs(triangleNormal.Dot(Vector3.Up));
      normalDot = (float)Math.Sqrt(normalDot);

      var noiseX = a_vector.X + worldPosition.X;
      var noiseY = a_vector.Z + worldPosition.Z;

      Color GrassColor = new("008013");
      Color ForestGrassColor = new("0b662f");

      if (TileData.vegetation == VegetationType.Forest) {
        GrassColor = ForestGrassColor;
      }

      Color DirtColor = new("662f0b");

      GrassColor += GrassColor * GD.Randf().Spread(0f, 1f, -0.03f, 0.03f);
      GrassColor += GrassColor * TerrainNoise.Noise.GetNoise2D(noiseX * 20, noiseY * 20).Spread(0f, 1f, -0.15f, 0.15f);

      DirtColor += DirtColor * GD.Randf().Spread(0f, 1f, -0.05f, 0.05f);
      DirtColor += DirtColor * TerrainNoise.Noise.GetNoise2D(noiseX * 40 + 12454, noiseY * 40 + 21455).Spread(0f, 1f, -0.20f, 0.20f);

      normalDot += TerrainNoise.Noise.GetNoise2D(noiseX * 40, noiseY * 40).Spread(0f, 1f, 0.0f, 0.25f);

      Color triangleColor = normalDot < 0.30f ? GrassColor : DirtColor;

      surfaceTool.SetColor(triangleColor);
      surfaceTool.SetNormal(triangleNormal);
      surfaceTool.SetUV(uv_a);
      surfaceTool.AddVertex(a_vector);

      surfaceTool.SetColor(triangleColor);
      surfaceTool.SetNormal(triangleNormal);
      surfaceTool.SetUV(uv_b);
      surfaceTool.AddVertex(b_vector);

      surfaceTool.SetColor(triangleColor);
      surfaceTool.SetNormal(triangleNormal);
      surfaceTool.SetUV(uv_c);
      surfaceTool.AddVertex(c_vector);
    }

    var mesh = surfaceTool.Commit();

    MeshInstance.CallDeferred(() => {
      MeshInstance.Mesh = mesh;

      GenerateInputMesh();
    });


    OnTileReady?.Invoke();
    this.CallDeferred(() => {
      OnTileReadyDeferred?.Invoke();
    });

    return;
  }

  private Vector2 VertexToUV(Vector3 vertex) {
    return new Vector2(vertex.X, vertex.Z) / 2 + new Vector2(0.5f, 0.5f);
  }

  private VertexData ProjectVertex(
    Vector3 vertex,
    MapTileData tile,
    float terrainScale,
    IEnumerable<(HexDirection, MapTileData)> neighbors
  ) {
    var totalContribution = 1.0f;
    var totalHeight = (float)tile.elevation;
    var totalSteepness = 0f;
    var steepnessContributes = 1f;

    foreach (var (direction, neighbor) in neighbors) {
      var distance = vertex.DistanceToEdge(direction, terrainScale); // from 0 to radius+
      var contribution = 0f;

      if (distance <= 0.05 * terrainScale) {
        contribution = 1;

      } else {
        distance /= terrainScale; // from 0 to 2 (at opposite edge)

        contribution = 2 - distance; // from 2 (at edge) to 0 (at opposite edge), 1 at center
        contribution -= 2f; // from 1 (at edge) to -1 (at opposite edge), 0 at center

        contribution = Numerics.LogisticSmooth(contribution, 1f - Constants.SLOPE_STEEPNESS) * 2;
      }

      if (tile.elevation != neighbor.elevation) {
        totalSteepness += contribution;
        steepnessContributes += contribution;
      }

      totalContribution += contribution;
      totalHeight += neighbor.elevation * contribution;
    }

    var baseHeight = totalHeight / totalContribution;
    var steepness = totalSteepness / steepnessContributes;

    vertex.Y = baseHeight * Constants.HEIGHT_SCALE;

    return new VertexData {
      position = vertex,
      steepness = steepness
    };
  }
  #endregion
}