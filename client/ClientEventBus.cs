using System;
using Godot;

namespace Client;

public partial class ClientEventBus : Node {
  //========================= Terrain Events =========================
  public event Action<MapTileData, MouseButton> OnTileClicked = default!;
  public event Action<MapTileData> OnTileRightClicked = default!;
  public event Action<MapTileData> OnTileLeftClicked = default!;
  public void TileClicked(MapTileData tile, MouseButton button) {
    OnTileClicked?.Invoke(tile, button);

    if (button == MouseButton.Left) {
      OnTileLeftClicked?.Invoke(tile);
    } else if (button == MouseButton.Right) {
      OnTileRightClicked?.Invoke(tile);
    }
  }

  public event Action<MapTileData> OnTileHovered = default!;
  public void TileHovered(MapTileData tile) => OnTileHovered?.Invoke(tile);
}