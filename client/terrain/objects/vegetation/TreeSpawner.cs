using Godot;
using Chickensoft.AutoInject;
using Chickensoft.Introspection;
using System;
using System.Collections.Generic;
using Tlib;

namespace Client.Terrain;

[Meta(typeof(IAutoConnect), typeof(IAutoNode))]
public partial class TreeSpawner : ObjectSpawner {
  [Node] protected MultiMeshInstance3D PineTreeMultimesh { get; set; } = default!;
  [Node] protected MultiMeshInstance3D OakTreeMultimesh { get; set; } = default!;
  [Export] private int maxPineTrees = 100;
  [Export] private int maxOakTrees = 3;

  protected override void SpawnObjects() {
    var vertices = tile.Vertices;

    var pineTreePositions = new HashSet<Vector3>();
    var oakTreePositions = new HashSet<Vector3>();
    var spawnedPositions = new HashSet<Vector3>();

    // generate random positions for pine trees
    for (int i = 0; i < maxPineTrees; i++) {
      var vertex = vertices.Random();
      var position = vertex.position;

      if (spawnedPositions.Contains(position)) { continue; }
      if (vertex.steepness > 0.3f) { continue; }

      if (tile.TileData.vegetation == VegetationType.Forest) {
        if (GD.Randf() > 0.2f) {
          pineTreePositions.Add(position);
          spawnedPositions.Add(position);
        }
      }
    }

    // generate random positions for oak trees
    for (int i = 0; i < maxOakTrees; i++) {
      var vertex = vertices.Random();
      var position = vertex.position;

      if (spawnedPositions.Contains(position)) { continue; }
      if (vertex.steepness > 0.3f) { continue; }
      oakTreePositions.Add(position);
      spawnedPositions.Add(position);

      if (tile.TileData.vegetation == VegetationType.Forest) {
        if (GD.Randf() > 0.99f) {
          oakTreePositions.Add(position);
          spawnedPositions.Add(position);
        }

      } else {
        if (GD.Randf() > 0.50f) {
          oakTreePositions.Add(position);
          spawnedPositions.Add(position);
        }
      }
    }

    PineTreeMultimesh.Multimesh.TransformFormat = MultiMesh.TransformFormatEnum.Transform3D;
    PineTreeMultimesh.Multimesh.InstanceCount = pineTreePositions.Count;

    OakTreeMultimesh.Multimesh.TransformFormat = MultiMesh.TransformFormatEnum.Transform3D;
    OakTreeMultimesh.Multimesh.InstanceCount = oakTreePositions.Count;

    int index = 0;

    foreach (var position in pineTreePositions) {
      var transform = Transform3D.Identity.Translated(position);

      var random_angle = GD.Randf() * Mathf.Pi * 2;

      transform = transform.RotatedLocal(new Vector3(0, 1, 0), random_angle);

      var random_scale = GD.Randf().Spread(0f, 1f, 1f, 2f);
      transform = transform.ScaledLocal(new Vector3(random_scale, random_scale, random_scale) * 0.2f);

      PineTreeMultimesh.Multimesh.SetInstanceTransform(index, transform);
      index++;
    }

    index = 0;
    foreach (var position in oakTreePositions) {
      var transform = Transform3D.Identity.Translated(position);

      var random_angle = GD.Randf() * Mathf.Pi * 2;

      transform = transform.RotatedLocal(new Vector3(0, 1, 0), random_angle);

      var random_scale = GD.Randf().Spread(0f, 1f, 1f, 2f);
      transform = transform.ScaledLocal(new Vector3(random_scale, random_scale, random_scale) * 0.09f);

      OakTreeMultimesh.Multimesh.SetInstanceTransform(index, transform);
      index++;
    }
  }
}
