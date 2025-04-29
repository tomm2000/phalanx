using Godot;
using Chickensoft.AutoInject;
using Chickensoft.Introspection;

namespace Client.Terrain;

[Meta(typeof(IAutoConnect), typeof(IAutoNode))]
public partial class TileSelector : Node3D {
  public override void _Notification(int what) => this.Notify(what);

  private Vector3 targetPosition = Vector3.Zero;
  private bool isMoving = false;

  public override void _Ready() {
    ClientEventBus.OnTileHovered += OnTileHovered;
  }

  public override void _Process(double delta) {
    // lerp to the target position
    if (isMoving) {
      Position = Position.Lerp(targetPosition, 0.1f);

      if (Position.DistanceTo(targetPosition) < 0.001f) {
        isMoving = false;
      }
    }
  }

  private void OnTileHovered(MapTileData data) {
    var position = data.coords.GridToWorld3D(Constants.TERRAIN_SCALE);
    position.Y = data.elevation * Constants.HEIGHT_SCALE;

    targetPosition = position;
    isMoving = true;
  }

}
