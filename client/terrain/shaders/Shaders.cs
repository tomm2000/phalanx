using System;
using Godot;

public enum TerrainShader {
  Standard,
  DebugNormals,
  DebugUVs,
  DebugEdgeDistances,
  Wireframe,
  DebugRiver,
  DebugFlow
}

public static class TerrainShaders {
  public static Shader? GetShader(TerrainShader shader) {
    return shader switch {
      TerrainShader.Standard => GD.Load<Shader>("uid://dw8sfu5i5p4as"),
      TerrainShader.Wireframe => GD.Load<Shader>("uid://bv1lc746rv046"),
      TerrainShader.DebugNormals => GD.Load<Shader>("uid://csxmk8i5rjf68"),
      TerrainShader.DebugUVs => GD.Load<Shader>("uid://c3pqpsf46oydf"),
      TerrainShader.DebugEdgeDistances => GD.Load<Shader>("uid://cmav5wqkg5jct"),
      TerrainShader.DebugRiver => GD.Load<Shader>("uid://dfa0jc6jte3js"),
      TerrainShader.DebugFlow => GD.Load<Shader>("uid://bpk8pperag7dl"),
      _ => throw new ArgumentOutOfRangeException(nameof(shader), shader, null),
    };
  }
}