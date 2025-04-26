using Godot;
using System;
using System.Collections.Generic;

public class Triangle3D {
  public Vector3 A { get; }
  public Vector3 B { get; }
  public Vector3 C { get; }

  public Triangle3D(Vector3 a, Vector3 b, Vector3 c) {
    A = a;
    B = b;
    C = c;
  }

  public override string ToString() {
    return $"Triangle3D(A: {A}, B: {B}, C: {C})";
  }
}

public static class Meshes {
  public static List<Triangle3D> CreateHexagonMesh(
    float radius,
    uint subdivisions = 3
  ) {
    var mainPoints = new List<Vector3>();
    var angleOffset = Mathf.Pi / 3;
    var angle = Mathf.Pi / 6; // pointy top
    var center = new Vector3(0, 0, 0);

    for (int i = 0; i < 6; i++) {
      var x = Mathf.Cos(angle) * radius;
      var z = Mathf.Sin(angle) * radius;
      mainPoints.Add(new Vector3(x, 0, z) + center);
      angle += angleOffset;
    }

    var triangles = new List<Triangle3D>();

    // create triangles
    for (var i = 0; i < 6; i++) {
      var triangle = new Triangle3D(mainPoints[i], mainPoints[(i + 1) % 6], center);

      SubdivideTriangle(
        triangle,
        center,
        triangles,
        subdivisions,
        0
      );
    }

    return triangles;
  }

  private static void SubdivideTriangle(
    Triangle3D triangle,
    Vector3 origin,
    List<Triangle3D> triangles,
    uint max_depth = 3,
    uint depth = 0
  ) {
    if (depth >= max_depth) {
      triangles.Add(triangle);
      return;
    }

    var a = triangle.A;
    var b = triangle.B;
    var c = triangle.C;

    var ab = (a + b) / 2;
    var bc = (b + c) / 2;
    var ca = (c + a) / 2;

    var triangle_1 = new Triangle3D(a, ab, ca);
    var triangle_2 = new Triangle3D(ab, b, bc);
    var triangle_3 = new Triangle3D(ca, bc, c);
    var triangle_4 = new Triangle3D(ab, bc, ca);

    SubdivideTriangle(triangle_1, origin, triangles, max_depth, depth + 1);
    SubdivideTriangle(triangle_2, origin, triangles, max_depth, depth + 1);
    SubdivideTriangle(triangle_3, origin, triangles, max_depth, depth + 1);
    SubdivideTriangle(triangle_4, origin, triangles, max_depth, depth + 1);
  }
}
