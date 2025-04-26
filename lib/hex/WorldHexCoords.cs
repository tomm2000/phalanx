using System;
using System.Linq;
using Godot;
using Tlib;

public static class WorldHexCoordsExtension {  /// <summary>
  /// The vertices of a generic hexagon at 0,0
  /// </summary>
  public static Vector2[] precalcualtedVertices = new Vector2[6];

  /// <summary>
  /// The edges of a generic hexagon at 0,0
  /// </summary>
  public static Vector4[] precalcualtedEdges = new Vector4[6];

  // ============ PRECALCULATED ============
  static WorldHexCoordsExtension() {
    precalcualtedVertices = CalculateVertices();
    precalcualtedEdges = CalculateEdges(precalcualtedVertices);
  }

  /// <summary>
  /// Calculate the vertices of a hexagon with the given radius
  /// NOTE: the hexagos are pointy-top
  /// </summary>

  //            1
  //          /   \
  //         2     0
  //         |  *  |
  //         3     5
  //          \   /
  //            4
  public static Vector2[] CalculateVertices(float radius = 1) {
    Vector2[] vertices = new Vector2[6];
    for (int i = 0; i < 6; i++) {
      float angle = - (MathF.PI / 6 + MathF.PI / 3 * i);
      //            ^ the - is required since z axis is inverted in Godot

      float x = radius * MathF.Cos(angle);
      float y = radius * MathF.Sin(angle);
      vertices[i] = new Vector2(x, y);
    }

    return vertices;
  }

  public static Vector4[] CalculateEdges(Vector2[] vertices) {
    Vector4[] edges = new Vector4[6];

    for (int i = 0; i < 6; i++) {
      Vector2 start = vertices[i];
      Vector2 end = vertices[Numerics.Wrap(i - 1, 6)];
      edges[i] = new Vector4(start.X, start.Y, end.X, end.Y);
    }

    return edges;
  }

  // ============ HELPERS ============
  private static HexCoords CubicRound(float q, float r, float s) {
    int q_rounded = (int)MathF.Round(q);
    int r_rounded = (int)MathF.Round(r);
    int s_rounded = (int)MathF.Round(s);

    float q_diff = MathF.Abs(q_rounded - q);
    float r_diff = MathF.Abs(r_rounded - r);
    float s_diff = MathF.Abs(s_rounded - s);

    if (q_diff > r_diff && q_diff > s_diff) {
      q_rounded = -r_rounded - s_rounded;
    } else if (r_diff > s_diff) {
      r_rounded = -q_rounded - s_rounded;
    } else {
      s_rounded = -q_rounded - r_rounded;
    }

    // return new HexCoords(q_rounded, r_rounded);
    return HexCoords.FromOffsetCoords(q_rounded, r_rounded);
  }

  // ============ CONVERTERS ============
  public static Vector2 GridToWorld(this HexCoords coords, float radius = 1) {
    var (q, r) = coords.ToAxialCoords();

    float x = radius * (MathF.Sqrt(3) * q + MathF.Sqrt(3) / 2 * r);
    float y = radius * (3f / 2 * r);
    return new Vector2(x, y);
  }

  public static Vector3 GridToWorld3D(this HexCoords coords, float radius = 1) {
    Vector2 world = coords.GridToWorld(radius);
    return new Vector3(world.X, 0, world.Y);
  }

  public static HexCoords WorldToGrid(this Vector2 world, float radius = 1) {
    float q = (MathF.Sqrt(3) / 3 * world.X - 1.0f / 3 * world.Y) / radius;
    float r = (                              2.0f / 3 * world.Y) / radius;
    return CubicRound(q, r, -q - r);
  }

  // ============ EDGES ============
  public static Vector2[] GetVertices(float radius = 1) {
    return [.. precalcualtedVertices.Select(vertex => {
      return vertex * radius;
    })];
  }

  public static Vector4[] GetEdges(float radius = 1) {
    return [.. precalcualtedEdges.Select(edge => {
      return edge * radius;
    })];
  }

  public static Vector4 GetEdge(HexDirection direction, float radius = 1) {
    return GetEdges(radius)[(int)direction];
  }

  public static float DistanceToEdge(this Vector2 coords, HexDirection direction, float radius = 1) {
    Vector4 edge = GetEdges(radius)[(int) direction];

    return Numerics.PointEdgeDistance(edge, coords);
  }

  public static float DistanceToEdge(this Vector3 coords, HexDirection direction, float radius = 1) {
    var coords2 = new Vector2(coords.X, coords.Z);
    return coords2.DistanceToEdge(direction, radius);
  }

  public static float MinDistanceFromEdges(this Vector2 coords, float radius = 1) {
    Vector4[] edges = GetEdges(radius);

    float minDistance = float.MaxValue;

    for (int i = 0; i < 6; i++) {
      float distance = Numerics.PointEdgeDistance(edges[i], coords);
      minDistance = MathF.Min(minDistance, distance);
    }

    return minDistance;
  }

  public static float MinDistanceFromEdges(this Vector3 coords, float radius = 1) {
    var coords2 = new Vector2(coords.X, coords.Z);
    return coords2.MinDistanceFromEdges(radius);
  }
}

public static class HexDebugExtension {
  private static void EdgeToDesmosPoint(this Vector4 edge, string name) {
    GD.Print($"P_{name} = ({edge.X.ToString("F7", System.Globalization.CultureInfo.InvariantCulture)},{edge.Y.ToString("F7", System.Globalization.CultureInfo.InvariantCulture)})");
    GD.Print($"Q_{name} = ({edge.Z.ToString("F7", System.Globalization.CultureInfo.InvariantCulture)},{edge.W.ToString("F7", System.Globalization.CultureInfo.InvariantCulture)})");
  }

  public static void EdgeToDesmosPoint(this Vector4 edge) {
    GD.Print($"P = ({edge.X.ToString("F7", System.Globalization.CultureInfo.InvariantCulture)},{edge.Y.ToString("F7", System.Globalization.CultureInfo.InvariantCulture)})");
    GD.Print($"Q = ({edge.Z.ToString("F7", System.Globalization.CultureInfo.InvariantCulture)},{edge.W.ToString("F7", System.Globalization.CultureInfo.InvariantCulture)})");
  }

  public static void EdgesToDesmosPoints(this Vector4[] edges) {
    for (int i = 0; i < edges.Length; i++) {
      edges[i].EdgeToDesmosPoint($"{i}");      
    }
  }

  public static void VertexToDesmosPoint(this Vector2 vertex, char? name = null) {
    var id = name == null ? "" : "_" + name;
    GD.Print($"P{id} = ({vertex.X.ToString("F7", System.Globalization.CultureInfo.InvariantCulture)},{vertex.Y.ToString("F7", System.Globalization.CultureInfo.InvariantCulture)})");
  }
}