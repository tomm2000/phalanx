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
  public float riverness;
  public Vector2 flowDirection;
}

[Meta(typeof(IAutoConnect), typeof(IAutoNode))]
public partial class StandardTile : Node3D {
  public override void _Notification(int what) => this.Notify(what);
  public static readonly string ScenePath = "uid://clkgqxom5swiy";

  [Node] private MeshInstance3D MeshInstance { get; set; } = default!;
  [Node] private CollisionShape3D CollisionShape { get; set; } = default!;
  [Node] private Area3D CollisionArea { get; set; } = default!;

  [Export] private NoiseTexture2D TerrainColorNoise { get; set; } = default!;
  [Export] private NoiseTexture2D RiverShapeNoise { get; set; } = default!;

  private SurfaceTool surfaceTool = new();
  private ShaderMaterial ShaderMaterial => (MeshInstance.MaterialOverride as ShaderMaterial)!;

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
    surfaceTool.SetCustomFormat(0, SurfaceTool.CustomFormat.RgbaFloat);

    var triangles = Meshes.CreateHexagonMesh(radius: terrainScale, Constants.TERRAIN_MESH_SUBDIVISIONS);

    foreach (var triangle in triangles) {
      var a = ProjectVertex(triangle.A, TileData, terrainScale, neighbors);
      var b = ProjectVertex(triangle.B, TileData, terrainScale, neighbors);
      var c = ProjectVertex(triangle.C, TileData, terrainScale, neighbors);

      a = CalculateRiverness(a, TileData);
      b = CalculateRiverness(b, TileData);
      c = CalculateRiverness(c, TileData);

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
      GrassColor += GrassColor * TerrainColorNoise.Noise.GetNoise2D(noiseX * 20, noiseY * 20).Spread(0f, 1f, -0.15f, 0.15f);

      DirtColor += DirtColor * GD.Randf().Spread(0f, 1f, -0.05f, 0.05f);
      DirtColor += DirtColor * TerrainColorNoise.Noise.GetNoise2D(noiseX * 40 + 12454, noiseY * 40 + 21455).Spread(0f, 1f, -0.20f, 0.20f);

      normalDot += TerrainColorNoise.Noise.GetNoise2D(noiseX * 40, noiseY * 40).Spread(0f, 1f, 0.0f, 0.25f);

      Color triangleColor = normalDot < 0.30f ? GrassColor : DirtColor;

      Color WaterColor = new("#3b8cba");
      WaterColor += WaterColor * GD.Randf().Spread(0f, 1f, -0.05f, 0.05f);
      // if ((a.riverness + b.riverness + c.riverness) / 3 >= 0.60f) {
      if (Math.Min(a.riverness, Math.Min(b.riverness, c.riverness)) >= 0.50f) {
        triangleColor = WaterColor;
      }

      surfaceTool.SetCustom(0, new Color(a.riverness, a.flowDirection.X, a.flowDirection.Y));
      // surfaceTool.SetCustom(0, new Color(a.riverness, 0, a.flowDirection.Y));
      // surfaceTool.SetCustom(0, new Color(a.riverness, a.flowDirection.X, 0.5f));
      surfaceTool.SetColor(triangleColor);
      surfaceTool.SetNormal(triangleNormal);
      surfaceTool.SetUV(uv_a);
      surfaceTool.AddVertex(a_vector);

      surfaceTool.SetCustom(0, new Color(b.riverness, b.flowDirection.X, b.flowDirection.Y));
      // surfaceTool.SetCustom(0, new Color(b.riverness, 0, b.flowDirection.Y));
      // surfaceTool.SetCustom(0, new Color(b.riverness, b.flowDirection.X, 0.5f));
      surfaceTool.SetColor(triangleColor);
      surfaceTool.SetNormal(triangleNormal);
      surfaceTool.SetUV(uv_b);
      surfaceTool.AddVertex(b_vector);

      surfaceTool.SetCustom(0, new Color(c.riverness, c.flowDirection.X, c.flowDirection.Y));
      // surfaceTool.SetCustom(0, new Color(c.riverness, 0, c.flowDirection.Y));
      // surfaceTool.SetCustom(0, new Color(c.riverness, c.flowDirection.X, 0.5f));
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
    var uv = new Vector2(vertex.X, vertex.Z) / 2 + new Vector2(0.5f, 0.5f);
    uv.X = Mathf.Clamp(uv.X, 0, 1);
    uv.Y = Mathf.Clamp(uv.Y, 0, 1);
    return uv;
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

  #region Rivers
  private VertexData CalculateRiverness(
    VertexData vertex,
    MapTileData tile
  ) {
    var (distance, flow) = CalculateRiverPosition(vertex, tile);

    var riverness = 1 - distance * 4;
    riverness = riverness * 2 - 1;
    riverness = Numerics.LogisticSmooth(riverness, 0.8f, 0.2f);
    riverness = Mathf.Clamp(riverness, 0, 1);

    vertex.position.Y -= Mathf.Min(riverness, 0.5f) * Constants.RIVER_HEIGHT_SCALE;

    // transform flow vector from [-1, 1] to [0, 1]
    flow = flow.Normalized();
    flow.X = (flow.X + 1) / 2;
    flow.Y = (flow.Y + 1) / 2;

    return new VertexData {
      position = vertex.position,
      steepness = vertex.steepness,
      riverness = riverness,
      flowDirection = flow
    };
  }

  private (float, Vector2) CalculateRiverPosition(
    VertexData vertex,
    MapTileData tile
  ) {
    // if the tile has no river, return 1 (no river)
    if (!tile.isRiver) {
      return (1, new Vector2(0, 0));
    }

    // if the river is only made of 1 segment, it is straight
      // if the tile has only one river in and no river out, it is a river end
    if (tile.riverInDirection.Length == 1 && tile.riverOutDirection.Length == 0) {
      return RiverEnd(vertex, tile, tile.riverInDirection[0]);

      // if the tile has no river in and only one river out, it is a river source
    } else if (tile.riverInDirection.Length == 0 && tile.riverOutDirection.Length == 1) {
      return RiverSource(vertex, tile, tile.riverOutDirection[0]);
    }

    // if the river is made of 2 or more segments, create all possible in-out combinations
    var riverSplines = new List<(HexDirection, HexDirection)>();
    foreach (var inDirection in tile.riverInDirection) {
      foreach (var outDirection in tile.riverOutDirection) {
        riverSplines.Add((inDirection, outDirection));
      }
    }

    var distance = float.MaxValue;
    var flow = new Vector2(0, 0);
    var flowCount = 0f;

    foreach (var (inDirection, outDirection) in riverSplines) {
      // if the river is made of 2 segments, and they are straight
      if (inDirection == outDirection.OppositeDirection()) {
        var (tmpDistance, tmpFlow) = RiverStraight(vertex, tile, inDirection, outDirection);
        distance = Math.Min(distance, tmpDistance);
        flow += tmpFlow;
        flowCount += 1;
      }

      // if the river is made of 2 segments, and they are small curves
      if (inDirection.StepAngle(outDirection) == 1) {
        var (tmpDistance, tmpFlow) = RiverSmallCurve(vertex, tile, inDirection, outDirection);
        distance = Math.Min(distance, tmpDistance);
        flow += tmpFlow;
        flowCount += 1;
      }

      // if the river is made of 2 segments, and they are big curves
      if (inDirection.StepAngle(outDirection) == 2) {
        var (tmpDistance, tmpFlow) = RiverBigCurve(vertex, tile, inDirection, outDirection);
        distance = Math.Min(distance, tmpDistance);
        flow += tmpFlow;
        flowCount += 1;
      }
    }

    return (distance, flow / flowCount);
  }

  private (float, Vector2) RiverSource(
    VertexData vertex,
    MapTileData tile,
    HexDirection direction
  ) {
    var edge = WorldHexCoordsExtension.GetEdge(direction);
    // anchor is the middle of the edge
    var anchorOut = Numerics.EdgeMidpoint(edge);
    var riverSegment = new Vector4(
      anchorOut.X,
      anchorOut.Y,
      0,
      0
    );

    var flow = (anchorOut - new Vector2(0, 0)).Normalized();

    var flowNormal = new Vector2(flow.Y, -flow.X);
    var worldPosition = TileData.coords.GridToWorld3D();
    var noiseX = vertex.position.X + worldPosition.X;
    var noiseY = vertex.position.Z + worldPosition.Z;
    var noise = RiverShapeNoise.Noise
      .GetNoise2D(noiseX * 25, noiseY * 25);

    noise *= 0.5f;
    var offset = flowNormal * noise;
    var offsetVertex = vertex.position.XZ() + offset;

    var distance = Numerics.PointEdgeDistance(riverSegment, offsetVertex);

    return (distance, flow);
  }

  private (float, Vector2) RiverEnd(
    VertexData vertex,
    MapTileData tile,
    HexDirection direction
  ) {
    var edge = WorldHexCoordsExtension.GetEdge(direction);
    // anchor is the middle of the edge
    var anchorIn = Numerics.EdgeMidpoint(edge);
    var riverSegment = new Vector4(
      0,
      0,
      anchorIn.X,
      anchorIn.Y
    );

    var flow = (new Vector2(0, 0) - anchorIn).Normalized();

    var flowNormal = new Vector2(flow.Y, -flow.X);
    var worldPosition = TileData.coords.GridToWorld3D();
    var noiseX = vertex.position.X + worldPosition.X;
    var noiseY = vertex.position.Z + worldPosition.Z;
    var noise = RiverShapeNoise.Noise
      .GetNoise2D(noiseX * 25, noiseY * 25);

    noise *= 0.5f;
    var offset = flowNormal * noise;
    var offsetVertex = vertex.position.XZ() + offset;

    var distance = Numerics.PointEdgeDistance(riverSegment, offsetVertex);

    return (distance, flow);
  }

  private (float, Vector2) RiverStraight(
    VertexData vertex,
    MapTileData tile,
    HexDirection inDirection,
    HexDirection outDirection
  ) {
    var edgeIn = WorldHexCoordsExtension.GetEdge(inDirection);
    var anchorIn = Numerics.EdgeMidpoint(edgeIn);

    var edgeOut = WorldHexCoordsExtension.GetEdge(outDirection);
    var anchorOut = Numerics.EdgeMidpoint(edgeOut);

    var flow = (anchorOut - anchorIn).Normalized();

    var flowNormal = new Vector2(flow.Y, -flow.X);
    var worldPosition = TileData.coords.GridToWorld3D();
    var noiseX = vertex.position.X + worldPosition.X;
    var noiseY = vertex.position.Z + worldPosition.Z;
    var noise = RiverShapeNoise.Noise
      .GetNoise2D(noiseX * 25, noiseY * 25);

    noise *= 0.5f;

    // offset the distance in the normal direction by the noise
    var offset = flowNormal * noise;
    var offsetVertex = vertex.position.XZ() + offset;

    var riverSegment = new Vector4(
      anchorIn.X,
      anchorIn.Y,
      anchorOut.X,
      anchorOut.Y
    );
    var distance = Numerics.PointEdgeDistance(riverSegment, offsetVertex);


    return (distance, flow);
  }

  private (float, Vector2) RiverSmallCurve(
    VertexData vertex,
    MapTileData tile,
    HexDirection inDirection,
    HexDirection outDirection
  ) {
    var curveCenter = Numerics.EdgeProjectionIntersection(
      WorldHexCoordsExtension.GetEdge(inDirection),
      WorldHexCoordsExtension.GetEdge(outDirection)
    );

    // Flow direction is the normalized vector from curve center to vertex
    var toVertex = vertex.position.XZ() - curveCenter;
    var tangent = toVertex.Normalized();

    // Determine if the river is turning left (CCW) or right (CW)
    bool isLeftTurn = ((int)outDirection - (int)inDirection + 6) % 6 < 3;

    Vector2 flow;
    if (isLeftTurn) {
      flow = new Vector2(-tangent.Y, tangent.X); // 90 degrees CCW
    } else {
      flow = new Vector2(tangent.Y, -tangent.X); // -90 degrees CW
    }

    var flowNormal = new Vector2(flow.Y, -flow.X);
    var worldPosition = TileData.coords.GridToWorld3D();
    var noiseX = vertex.position.X + worldPosition.X;
    var noiseY = vertex.position.Z + worldPosition.Z;
    var noise = RiverShapeNoise.Noise
      .GetNoise2D(noiseX * 25, noiseY * 25);

    noise *= 0.5f;
    var offset = flowNormal * noise;
    var offsetVertex = vertex.position.XZ() + offset;

    var distance = offsetVertex.DistanceTo(curveCenter);
    distance = Math.Abs(distance - 0.5f);
    
    return (distance, flow);
  }

  private (float, Vector2) RiverBigCurve(
    VertexData vertex,
    MapTileData tile,
    HexDirection inDirection,
    HexDirection outDirection
  ) {
    var curveCenter = Numerics.EdgeProjectionIntersection(
      WorldHexCoordsExtension.GetEdge(inDirection),
      WorldHexCoordsExtension.GetEdge(outDirection)
    );
    
    var toVertex = vertex.position.XZ() - curveCenter;
    var tangent = toVertex.Normalized();

    // Determine if the river is turning left (CCW) or right (CW)
    bool isLeftTurn = ((int)outDirection - (int)inDirection + 6) % 6 < 3;

    Vector2 flow;
    if (isLeftTurn) {
      flow = new Vector2(-tangent.Y, tangent.X); // 90 degrees CCW
    } else {
      flow = new Vector2(tangent.Y, -tangent.X); // -90 degrees CW
    }

    var flowNormal = new Vector2(flow.Y, -flow.X);
    var worldPosition = TileData.coords.GridToWorld3D();
    var noiseX = vertex.position.X + worldPosition.X;
    var noiseY = vertex.position.Z + worldPosition.Z;
    var noise = RiverShapeNoise.Noise
      .GetNoise2D(noiseX * 25, noiseY * 25);

    noise *= 0.5f;
    var offset = flowNormal * noise;
    var offsetVertex = vertex.position.XZ() + offset;

    var distance = offsetVertex.DistanceTo(curveCenter);
    distance = Math.Abs(distance - 1.5f);

    return (distance, flow);
  }
  #endregion
}