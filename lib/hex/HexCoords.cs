using Godot;
using System;
using MessagePack;
using MessagePack.Resolvers;

using System.Collections.Generic;

//           / \
//          2   1
//        |       |
//        3       0
//        |       |
//          4   5
//           \ /

/// <summary>
/// The directions of the edges of a hexagon, they rotate counterclockwise same as angle degrees/radians.
/// </summary>
public enum HexDirection { East, NorthEast, NorthWest, West, SouthWest, SouthEast }

[MessagePackObject(AllowPrivate = true)]
public partial struct HexCoords {
  [Key(0)] private readonly int q;
  [Key(1)] private readonly int r;

  #region Creation
  // for MessagePack
  public HexCoords() {}

  private HexCoords(int q, int r) {
    this.q = q;
    this.r = r;
  }

  /// <summary>
  /// By default the offset coordinates are odd-r.
  /// </summary>
  public static HexCoords FromOffsetCoords(int x, int y) {
    // return new OffsetCoords(x, y).IntoHexCoords();

    return new HexCoords(x - (y - (y & 1)) / 2, y);
  }

  public static HexCoords FromAxialCoords(int q, int r) {
    // return new AxialCoords(q, r).IntoHexCoords();

    return new HexCoords(q, r);
  }

  public static HexCoords FromCubicCoords(int q, int r, int s) {
    // return new CubicCoords(q, r, s).IntoHexCoords();

    return new HexCoords(q, r);
  }

  public static HexCoords Random(int range) {
    var q = GD.RandRange(-range, range);
    var r = GD.RandRange(-range, range);
    return new HexCoords(q, r);
  }
  #endregion

  #region Conversion
  public OffsetCoords ToOffsetCoords() {
    var x = q + (r - (r & 1)) / 2;
    var y = r;
    return new OffsetCoords(x, y);
  }

  public AxialCoords ToAxialCoords() {
    return new AxialCoords(q, r);
  }

  public CubicCoords ToCubicCoords() {
    return new CubicCoords(q, r, -q - r);
  }
  #endregion

  #region Equality
  public static bool operator ==(HexCoords a, HexCoords b) {
    return a.q == b.q && a.r == b.r;
  }

  public static bool operator !=(HexCoords a, HexCoords b) {
    return !(a == b);
  }

  public override readonly bool Equals(object? obj) {
    return obj is HexCoords coords && this == coords;
  }

  public override readonly int GetHashCode() {
    return HashCode.Combine(q, r);
  }
  #endregion

  #region Operators
  public static HexCoords operator +(HexCoords a, HexCoords b) {
    return new HexCoords(a.q + b.q, a.r + b.r);
  }

  public static HexCoords operator -(HexCoords a, HexCoords b) {
    return new HexCoords(a.q - b.q, a.r - b.r);
  }
  
  public static int Distance(HexCoords a, HexCoords b) {
    return (Math.Abs(a.q - b.q) + Math.Abs(a.q + a.r - b.q - b.r) + Math.Abs(a.r - b.r)) / 2;
  }

  public readonly int DistanceTo(HexCoords other) => Distance(this, other);
  #endregion

  #region Neighbors
  public readonly static HexCoords[] directions = [
    // follow the order of HexDirection
    new HexCoords(1, 0), // East
    new HexCoords(1, -1), // NorthEast
    new HexCoords(0, -1), // NorthWest
    new HexCoords(-1, 0), // West
    new HexCoords(-1, 1),  // SouthWest
    new HexCoords(0, 1), // SouthEast
  ];

  public readonly HexCoords Neighbor(HexDirection direction) {
    return this + directions[(int) direction];
  }

  public readonly HexCoords[] Neighbors() {
    var neighbors = new HexCoords[6];
    for (int i = 0; i < 6; i++) {
      neighbors[i] = Neighbor((HexDirection) i);
    }
    return neighbors;
  }

  public readonly (HexDirection, HexCoords)[] NeighborsWithDirections() {
    var neighbors = new (HexDirection, HexCoords)[6];
    for (int i = 0; i < 6; i++) {
      neighbors[i] = ((HexDirection) i, Neighbor((HexDirection) i));
    }
    return neighbors;
  }

  public readonly List<HexCoords> Range(int radius) {
    var results = new List<HexCoords>();

    for (int dx = -radius; dx <= radius; dx++) {
      for (int dy = Math.Max(-radius, -dx - radius); dy <= Math.Min(radius, -dx + radius); dy++) {
        results.Add(this + new HexCoords(dx, dy));
      }
    }

    return results;
  }
  #endregion

  #region String
  public override string ToString() { return $"[{q}, {r}]"; }
  #endregion

  #region Serialization
  public byte[] pack() { return MessagePackSerializer.Serialize(this, StandardResolverAllowPrivate.Options); }
  public static HexCoords unpack(byte[] data) { return MessagePackSerializer.Deserialize<HexCoords>(data, StandardResolverAllowPrivate.Options); }

  public static implicit operator byte[](HexCoords data) { return data.pack(); }
  public static implicit operator HexCoords(byte[] data) { return unpack(data); }
  #endregion
}


public interface IntoHexCoords {
  HexCoords IntoHexCoords();
}

public readonly struct CubicCoords(int q, int r, int s) : IntoHexCoords {
  public readonly int q = q, r = r, s = s;

  public HexCoords IntoHexCoords() {
    // return new HexCoords(q, r);
    return HexCoords.FromCubicCoords(q, r, s);
  }

  internal void Deconstruct(out int q, out int r, out int s) {
    (q, r, s) = (this.q, this.r, this.s);
  }

  public override string ToString() { return $"[{q}, {r}, {s}]"; }
}

public readonly struct AxialCoords(int q, int r) : IntoHexCoords {
  public readonly int q = q, r = r;

  public HexCoords IntoHexCoords() {
    // return new HexCoords(q, r);
    return HexCoords.FromAxialCoords(q, r);
  }

  internal void Deconstruct(out int q, out int r) {
    (q, r) = (this.q, this.r);
  }
  
  public override string ToString() { return $"[{q}, {r}]"; }
}

public readonly struct OffsetCoords(int x, int y) : IntoHexCoords {
  public readonly int x = x, y = y;

  /// <summary>
  /// https://www.redblobgames.com/grids/hexagons/#conversions
  /// </summary>
  public HexCoords IntoHexCoords() {
    // var q = x - (y - (y & 1)) / 2;
    // var r = y;
    // return new HexCoords(q, r);
    return HexCoords.FromOffsetCoords(x, y);
  }

  internal void Deconstruct(out int x, out int y) {
    (x, y) = (this.x, this.y);
  }
  public override string ToString() { return $"[{x}, {y}]"; }
}
