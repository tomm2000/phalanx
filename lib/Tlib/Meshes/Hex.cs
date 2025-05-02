using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public struct HexVertexIndex {
  public ushort layer;
  public ushort step;

  public override string ToString() {
    return $"[{layer}, {step}]";
  }

  public static HexVertexIndex FromExtended(ushort face, ushort layer, ushort step) {
    if (face == 5 && step == layer) {
      // this is the last vertex of the last layer, which is shared with the first face
      return new HexVertexIndex {
        layer = layer,
        step = 0,
      };
    }

    return new HexVertexIndex {
      layer = layer,
      step = (ushort)(step + face * layer),
    };
  }
}

public struct TriangleIndices {
  public HexVertexIndex a;
  public HexVertexIndex b;
  public HexVertexIndex c;

  public override string ToString() {
    return $"[{a}, {b}, {c}]";
  }
}


public static class TlibHexMesher {
  private static Dictionary<int, List<HexVertexIndex>> indexCache = [];

  public static Dictionary<HexVertexIndex, Vector3> GenerateVertices(
    int strips,
    float scale
  ) {
    var vertices = new Dictionary<HexVertexIndex, Vector3>();
    var origin = new Vector3(0, 0, 0);
    var angleOffset = Math.PI / 6;
    var angleStep = Math.PI / 3;
    var radiusStep = scale / strips;

    // the cheese formula: 3S(S+1) + 1
    var expectedVertices = 3 * strips * (strips + 1) + 1;

    for (ushort face = 0; face < 6; face++) {
      var angleA = angleStep * face + angleOffset;
      var angleB = angleStep * (face + 1) + angleOffset;

      for (ushort layer = 0; layer <= strips; layer++) {
        var layerMult = radiusStep * layer;

        var pointA = new Vector3(
          origin.X + (float)Math.Cos(angleA) * layerMult,
          origin.Y,
          origin.Z + (float)Math.Sin(angleA) * layerMult
        );

        var pointB = new Vector3(
          origin.X + (float)Math.Cos(angleB) * layerMult,
          origin.Y,
          origin.Z + (float)Math.Sin(angleB) * layerMult
        );

        for (ushort step = 0; step <= layer; step++) {
          if (face == 5 && step == layer) {
            // skip the last vertices of the last layer (they are shared with the first face)
            continue;
          }

          // find point between pointA and pointB
          var stepMult = (layer == 0) ? 0f : (float)step / layer;

          var point = new Vector3(
            pointA.X + (pointB.X - pointA.X) * stepMult,
            pointA.Y + (pointB.Y - pointA.Y) * stepMult,
            pointA.Z + (pointB.Z - pointA.Z) * stepMult
          );

          var index = HexVertexIndex.FromExtended(face, layer, step);

          if (vertices.ContainsKey(index)) { continue; }

          vertices[index] = point;
        }
      }
    }

    if (vertices.Count != expectedVertices) {
      throw new Exception($"Expected {expectedVertices} vertices, got {vertices.Count}");
    }

    return vertices;
  }

  public static List<Triangle3D> GenerateTriangles(
    int strips,
    float scale
  ) {
    var triangles = new List<Triangle3D>();
    var vertices = GenerateVertices(strips, scale);

    for (ushort face = 0; face < 6; face++) {
      for (ushort layer = 0; layer <= strips; layer++) {
        for (ushort n = 0; n < layer * 2 - 1; n++) {
          ushort i = (ushort)Math.Floor(n / 2f);

          HexVertexIndex indexA;
          HexVertexIndex indexB;
          HexVertexIndex indexC;

          if (n % 2 == 0) {
            // even triangle (normal orientation)

            indexA = HexVertexIndex.FromExtended(face, layer, i);
            indexB = HexVertexIndex.FromExtended(face, layer, (ushort)(i + 1));
            indexC = HexVertexIndex.FromExtended(face, (ushort)(layer - 1), i);
          } else {
            // odd triangle (inverted orientation)

            indexA = HexVertexIndex.FromExtended(face, layer, (ushort)(i + 1));
            indexB = HexVertexIndex.FromExtended(face, (ushort)(layer - 1), (ushort)(i + 1));
            indexC = HexVertexIndex.FromExtended(face, (ushort)(layer - 1), i);
          }

          var a = vertices[indexA];
          var b = vertices[indexB];
          var c = vertices[indexC];

          var triangle = new Triangle3D(a, b, c);
          triangles.Add(triangle);
        }
      }
    }

    return triangles;
  }

  // anchors are the geometric vertices of the hexagon
  public static List<HexVertexIndex> GetHexAnchorIndices(
    int strips
  ) {
    var anchors = new List<HexVertexIndex>();

    for (ushort face = 0; face < 6; face++) {
      var index = new HexVertexIndex {
        layer = (ushort)strips,
        step = (ushort)(face * strips)
      };

      anchors.Add(index);
    }

    return anchors;
  }

  public static List<HexVertexIndex> GetHexEdgeIndices(
    int strips
  ) {
    var edgeIndices = new List<HexVertexIndex>();
    for (ushort face = 0; face < 6; face++) {
      for (ushort step = 0; step < strips; step++) {
        var index = new HexVertexIndex {
          layer = (ushort)strips,
          step = (ushort)(face * strips + step)
        };
        edgeIndices.Add(index);
      }
    }
    return edgeIndices;
  }
}
