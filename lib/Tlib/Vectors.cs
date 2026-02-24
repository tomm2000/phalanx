using System;
using Godot;

static class Vectors {
  public static Vector3 ExtendZ(this Vector2 vector, float z) => new(vector.X, vector.Y, z);
  public static Vector3 ExtendY(this Vector2 vector, float y) => new(vector.X, y, vector.Y);
  public static Vector3 ExtendX(this Vector2 vector, float x) => new(x, vector.X, vector.Y);

  public static Vector3 WithX(this Vector3 vector, float x) {
    vector.X = x;
    return vector;
  }
  public static Vector3 WithY(this Vector3 vector, float y) {
    vector.Y = y;
    return vector;
  }
  public static Vector3 WithZ(this Vector3 vector, float z) {
    vector.Z = z;
    return vector;
  }

  public static void SetX(this ref Vector3 vector, float x) {
    vector.X = x;
  }
  public static void SetY(this ref Vector3 vector, float y) {
    vector.Y = y;
  }
  public static void SetZ(this ref Vector3 vector, float z) {
    vector.Z = z;
  }
}