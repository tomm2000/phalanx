using System;
using Godot;

public enum PlayerColor {
  Red,
  Green,
  Blue,
  Yellow,
  Purple,
  Orange,
  Pink,
  White,
  Black,
  Gray
}

public static class PlayerColorExtensions {
  public static Color ToColor(this PlayerColor color) {
    return color switch {
      PlayerColor.Red => new Color(1, 0, 0),
      PlayerColor.Green => new Color(0, 1, 0),
      PlayerColor.Blue => new Color(0, 0, 1),
      PlayerColor.Yellow => new Color(1, 1, 0),
      PlayerColor.Purple => new Color(1, 0, 1),
      PlayerColor.Orange => new Color(1, 0.5f, 0),
      PlayerColor.Pink => new Color(1, 0.75f, 0.8f),
      PlayerColor.White => new Color(1, 1, 1),
      PlayerColor.Black => new Color(0, 0, 0),
      PlayerColor.Gray => new Color(0.5f, 0.5f, 0.5f),
      _ => throw new ArgumentOutOfRangeException(nameof(color), color, null)
    };
  }
}