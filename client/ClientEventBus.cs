using System;
using Godot;

namespace Client;

public static class ClientEventBus {
  //========================= Terrain Events =========================
  public static event Action<MapTileData, MouseButton> OnTileClicked = default!;
  public static event Action<MapTileData> OnTileRightClicked = default!;
  public static event Action<MapTileData> OnTileLeftClicked = default!;
  public static void TileClicked(MapTileData tile, MouseButton button) {
    OnTileClicked?.Invoke(tile, button);

    if (button == MouseButton.Left) {
      OnTileLeftClicked?.Invoke(tile);
    } else if (button == MouseButton.Right) {
      OnTileRightClicked?.Invoke(tile);
    }
  }

  public static event Action<MapTileData> OnTileHovered = default!;
  public static void TileHovered(MapTileData tile) => OnTileHovered?.Invoke(tile);
}