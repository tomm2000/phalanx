using Godot;
using Chickensoft.AutoInject;
using Chickensoft.Introspection;
using System;

namespace Client.Terrain;

[Meta(typeof(IAutoConnect), typeof(IAutoNode))]
public partial class ObjectSpawner : Node3D {
  public override void _Notification(int what) => this.Notify(what);

  [Node] protected Node3D MultiMeshContainer { get; set; } = default!;
  protected ITerrainTile tile = default!;

  public override void _Ready() {
    tile = GetParent<ITerrainTile>();

    tile.OnTileReady += SpawnObjects;
  }

  protected virtual void SpawnObjects() {
    throw new NotImplementedException("SpawnObjects must be implemented in derived classes.");
  }
}