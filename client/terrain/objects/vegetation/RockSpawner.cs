using Godot;
using Chickensoft.AutoInject;
using Chickensoft.Introspection;
using System;
using System.Collections.Generic;
using Tlib;

namespace Client.Terrain;

[Meta(typeof(IAutoConnect), typeof(IAutoNode))]
public partial class RockSpawner : ObjectSpawner {
  [Node] protected MultiMeshInstance3D NormalRockMultimesh { get; set; } = default!;
  [Export] private int maxRocks = 10;

  protected override void SpawnObjects() {
    var vertices = tile.Vertices;

    var spawnedPositions = new HashSet<Vector3>();

    for (int i = 0; i < maxRocks; i++) {
      var vertex = vertices.Random();
      if (vertex.riverness > 0.3f) { continue; }
      var position = vertex.position;

      spawnedPositions.Add(position);
    }

    NormalRockMultimesh.Multimesh.TransformFormat = MultiMesh.TransformFormatEnum.Transform3D;
    NormalRockMultimesh.Multimesh.InstanceCount = spawnedPositions.Count;

    int index = 0;
    foreach (var position in spawnedPositions) {
      var transform = Transform3D.Identity.Translated(position);

      var random_angle_x = GD.Randf() * Mathf.Pi * 2;
      var random_angle_y = GD.Randf() * Mathf.Pi * 2;
      var random_angle_z = GD.Randf() * Mathf.Pi * 2;

      transform = transform.RotatedLocal(new Vector3(1, 0, 0), random_angle_x);
      transform = transform.RotatedLocal(new Vector3(0, 1, 0), random_angle_y);
      transform = transform.RotatedLocal(new Vector3(0, 0, 1), random_angle_z);

      var random_scale = GD.Randf().Spread(0f, 1f, 0.2f, 1.7f);
      random_scale *= random_scale;
      transform = transform.ScaledLocal(new Vector3(random_scale, random_scale, random_scale) * 0.05f);

      NormalRockMultimesh.Multimesh.SetInstanceTransform(index, transform);
      index++;
    }
  }
}
