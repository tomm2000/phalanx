using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Chickensoft.AutoInject;
using Chickensoft.Introspection;
using Client.Terrain;
using Godot;
using Tlib;
using Tlib.Nodes;

namespace Client;

[Meta(typeof(IAutoConnect), typeof(IAutoNode))]
public partial class TerrainTile : Node3D, ITerrainTile {
  public override void _Notification(int what) => this.Notify(what);
  public static readonly string ScenePath = "uid://bmydftsu36dtn";

  #region public interface
  public MapTileData TileData { get; private set; } = default!;
  public IEnumerable<VertexData> Vertices => vertices.Values;
  public event Action? OnTileReady;
  #endregion

  #region private properties
  private Dictionary<HexVertexIndex, VertexData> vertices = new();
  private SurfaceTool surfaceTool = new();
  #endregion

  #region export properties
  [ExportGroup(name: "terrain")]
  [Export] NoiseTexture2D TerrainColorNoise { get; set; } = default!;

  [ExportGroup(name: "river")]
  [Export] NoiseTexture2D RiverShapeNoise { get; set; } = default!;
  [Export] Curve RiverBankShape { get; set; } = default!;
  [Export(PropertyHint.Range, "0.1, 1.0, 0.1")] float RiverWidth { get; set; } = 0.25f;
  [Export(PropertyHint.Range, "0.1, 1.0, 0.1")] float RiverNoiseImpact { get; set; } = 0.4f;

  [ExportGroup(name: "colors")]
  [Export] Color GrassColor { get; set; } = new("008013");
  [Export] Color ForestColor { get; set; } = new("0b662f");
  [Export] Color DirtColor { get; set; } = new("662f0b");
  [Export] Color WaterColor { get; set; } = new("#3b8cba");
  #endregion

  public static TerrainTile Instantiate(MapTileData tileData) {
    var scene = GD.Load<PackedScene>(ScenePath);
    var instance = scene.Instantiate<TerrainTile>();
    instance.TileData = tileData;

    return instance;
  }

  #region Nodes
  [Node] MeshInstance3D MeshInstance { get; set; } = default!;
  [Node] CollisionShape3D CollisionShape { get; set; } = default!;
  [Node] Area3D CollisionArea { get; set; } = default!;
  [Dependency] StandardTerrain Terrain => this.DependOn<StandardTerrain>();
  [Dependency] ClientEventBus ClientEventBus => this.DependOn<ClientEventBus>();
  #endregion

  public void OnResolved() {
    CollisionArea.InputEvent += OnInputEvent;
    Position = TileData.coords.GridToWorld3D();
  }

  public void SetShader(TerrainShader shader) {
    var loadedShader = TerrainShaders.GetShader(shader);
    var shaderMaterial = new ShaderMaterial {
      Shader = loadedShader
    };
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

  #region Mesh Generation
  public void GenerateSurface(
    IEnumerable<(HexDirection, MapTileData)> neighbors
  ) {
    var strips = Constants.TERRAIN_MESH_RESOLUTION;

    surfaceTool.Clear();
    surfaceTool.Begin(Mesh.PrimitiveType.Triangles);
    surfaceTool.SetCustomFormat(0, SurfaceTool.CustomFormat.RgbaFloat);

    var worldPosition = TileData.coords.GridToWorld3D();

    // ----------- 0. generate the vertices ---------------
    var vertices = TlibHexMesher.GenerateVertices(strips, 1);
    this.vertices = vertices;

    // ----------- 1. apply the vertex pipeline ---------------
    foreach (var (index, vertex) in vertices) {
      var updatedVertex = ApplyVertexPipeline(
        vertex,
        index,
        neighbors
      );

      vertices[index] = updatedVertex;
    }

    // ----------- 2. generate the triangles ---------------
    var triangles = TlibHexMesher.GenerateTriangleIndices(strips);

    // ----------- 3. add the triangles to the surface tool
    foreach (var triangle in triangles) {
      var vertexA = vertices[triangle.a];
      var vertexB = vertices[triangle.b];
      var vertexC = vertices[triangle.c];

      var triangleNormal = (vertexA.position - vertexB.position).Cross(vertexC.position - vertexA.position).Normalized();
      var triangleColor = GetVertexColor(vertexA, vertexB, vertexC, triangleNormal, worldPosition);

      surfaceTool.SetCustom(0, new Color(vertexA.riverFactor, vertexA.riverFlowDirection.X, vertexA.riverFlowDirection.Y));
      surfaceTool.SetColor(triangleColor);
      surfaceTool.SetUV(vertexA.UV);
      surfaceTool.SetNormal(triangleNormal);
      surfaceTool.AddVertex(vertexA.position);

      surfaceTool.SetCustom(0, new Color(vertexB.riverFactor, vertexB.riverFlowDirection.X, vertexB.riverFlowDirection.Y));
      surfaceTool.SetColor(triangleColor);
      surfaceTool.SetUV(vertexB.UV);
      surfaceTool.SetNormal(triangleNormal);
      surfaceTool.AddVertex(vertexB.position);

      surfaceTool.SetCustom(0, new Color(vertexC.riverFactor, vertexC.riverFlowDirection.X, vertexC.riverFlowDirection.Y));
      surfaceTool.SetColor(triangleColor);
      surfaceTool.SetUV(vertexC.UV);
      surfaceTool.SetNormal(triangleNormal);
      surfaceTool.AddVertex(vertexC.position);
    }

    // ----------- 4. generate the mesh --------------------
    var mesh = surfaceTool.Commit();

    Terrain.TerrainQueueExecutor.Add(() => {
      MeshInstance.Mesh = mesh;
      GenerateInputMesh();
      OnTileReady?.Invoke();
    });
  }

  public VertexData ApplyVertexPipeline(
    VertexData vertex,
    HexVertexIndex index,
    IEnumerable<(HexDirection, MapTileData)> neighbors
  ) {
    // ----------- 1. vertex UV ---------------
    vertex = SetVertexUV(vertex);

    // ----------- 2. vertex elevation ---------------
    vertex = SetVertexElevation(
      index,
      vertex,
      neighbors
    );

    // ----------- 3. vertex river ---------------
    vertex = SetVertexRiver(vertex);

    // ----------- (last) 4. vertex color ---------------

    return vertex;
  }

  // FIXME: rework color selection to consider biome, vegetation, etc.
  private Color GetVertexColor(
    VertexData vertexA,
    VertexData vertexB,
    VertexData vertexC,
    Vector3 triangleNormal,
    Vector3 worldPosition
  ) {
    Color color;
    var noiseX = vertexA.position.X + worldPosition.X;
    var noiseY = vertexA.position.Z + worldPosition.Z;

    // if the tile is water, return the water color
    if (Math.Min(vertexA.riverFactor, Math.Min(vertexB.riverFactor, vertexC.riverFactor)) >= 0.50f) {
      color = WaterColor + WaterColor * GD.Randf().Spread(0f, 1f, -0.05f, 0.05f);
      return color;
    }

    float normalDot = 1 - Math.Abs(triangleNormal.Dot(Vector3.Up));
    normalDot = (float)Math.Sqrt(normalDot);
    normalDot += TerrainColorNoise.Noise.GetNoise2D(noiseX * 40, noiseY * 40).Spread(0f, 1f, 0.0f, 0.25f);

    // if normalDot is less than 0.3, its a slope
    if (normalDot > 0.3f) {
      color = DirtColor + DirtColor * GD.Randf().Spread(0f, 1f, -0.05f, 0.05f);
      color += DirtColor * TerrainColorNoise.Noise.GetNoise2D(noiseX * 40 + 12454, noiseY * 40 + 21455).Spread(0f, 1f, -0.20f, 0.20f);
      return color;
    }

    // else, its grass
    color = GrassColor;
    if (TileData.vegetation == VegetationType.Forest) {
      color = ForestColor;
    }

    color += color * GD.Randf().Spread(0f, 1f, -0.03f, 0.03f);
    color += color * TerrainColorNoise.Noise.GetNoise2D(noiseX * 20, noiseY * 20).Spread(0f, 1f, -0.15f, 0.15f);

    return color;
  }

  // FIXME: consider scale
  private VertexData SetVertexUV(VertexData vertex) {
    var uv = new Vector2(vertex.position.X, vertex.position.Z) / 2 + new Vector2(0.5f, 0.5f);
    uv.X = Mathf.Clamp(uv.X, 0, 1);
    uv.Y = Mathf.Clamp(uv.Y, 0, 1);
    vertex.UV = uv;
    return vertex.With(
      UV: uv
    );
  }

  // FIXME: consider scale
  private VertexData SetVertexElevation(
    HexVertexIndex index,
    VertexData vertex,
    IEnumerable<(HexDirection, MapTileData)> neighbors
  ) {
    var tile = TileData;
    var scale = Constants.TERRAIN_SCALE;

    var totalContribution = 1.0f;
    var totalHeight = (float)tile.elevation;
    var totalSteepness = 0f;
    var steepnessContributes = 1f;

    foreach (var (direction, neighbor) in neighbors) {
      var distance = vertex.position.DistanceToEdge(direction, scale); // from 0 to radius+
      var contribution = 0f;

      if (distance <= 0.05 * scale) {
        contribution = 1;

      } else {
        distance /= scale; // from 0 to 2 (at opposite edge)

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

    vertex.position.Y = baseHeight * Constants.HEIGHT_SCALE;

    return vertex.With(
      position: vertex.position,
      steepness: steepness
    );
  }

  #endregion

  // FIXME: consider scale
  #region Rivers
  private VertexData SetVertexRiver(
    VertexData vertex
  ) {
    var tile = TileData;
    var (distance, flow) = CalculateRiverPosition(vertex, tile);

    var riverness = 1 - Mathf.Clamp(distance / RiverWidth, 0, 1);
    riverness = 1 - RiverBankShape.Sample(riverness);

    vertex.position.Y -= Mathf.Min(riverness, 0.5f) * Constants.RIVER_HEIGHT_SCALE;

    // transform flow vector from [-1, 1] to [0, 1]
    flow = flow.Normalized();
    flow.X = (flow.X + 1) / 2;
    flow.Y = (flow.Y + 1) / 2;

    return vertex.With(
      riverFactor: riverness,
      riverFlowDirection: flow
    );
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

    noise *= RiverNoiseImpact;
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

    noise *= RiverNoiseImpact;
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

    noise *= RiverNoiseImpact;

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

    noise *= RiverNoiseImpact;
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

    noise *= RiverNoiseImpact;
    var offset = flowNormal * noise;
    var offsetVertex = vertex.position.XZ() + offset;

    var distance = offsetVertex.DistanceTo(curveCenter);
    distance = Math.Abs(distance - 1.5f);

    return (distance, flow);
  }
  #endregion
}
