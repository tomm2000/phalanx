using Godot;
using System;

public partial class DynamicMultiMesh : MultiMeshInstance3D {
  [Export] private Mesh mesh = default!;

  public override void _Ready() {
    var multimesh = new MultiMesh {
      Mesh = mesh,
      TransformFormat = MultiMesh.TransformFormatEnum.Transform3D,
      InstanceCount = 0
    };

    Multimesh = multimesh;
  }
}
