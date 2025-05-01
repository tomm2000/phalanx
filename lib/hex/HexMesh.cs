using System.Collections.Generic;
using Godot;

public class HexMesh {
  private Dictionary<(uint, uint), Vector3> vertices = [];

  private uint strips;
  private uint scale;

  public HexMesh(uint strips = 3, uint scale = 1) {
    this.strips = strips;
    this.scale = scale;
  }

  private List<(uint, uint)> GenerateIndices() {
    var indices = new List<(uint, uint)>();

    var maxIndex = strips * 2; // == 6

    for (uint layer = 0; layer <= maxIndex; layer++) {
      var nDivs = maxIndex - System.Math.Abs((int)maxIndex - (int)layer) + 1; // 3, 4, 5, 6, 5, 4, 3
      for (uint i = 0; i < nDivs; i++) {
        indices.Add((layer, i));
      }
    }

    return indices;
  }
}