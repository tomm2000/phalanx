using System;
using System.Collections;
using System.Collections.Generic;

public class MapData {
  private Dictionary<HexCoords, MapTileData> tileDict = [];

  public MapData(IEnumerable<MapTileData> tiles) {
    foreach (var tile in tiles) {
      tileDict[tile.coords] = tile;
    }
  }

  public IEnumerable<MapTileData> Tiles => tileDict.Values;

  /// <summary>
  /// Get the tile at the given coordinates.
  /// Returns null if no tile is found at the coordinates.
  /// </summary>
  public MapTileData? GetTileAt(HexCoords coords) {
    if (tileDict.TryGetValue(coords, out var tile)) {
      return tile;
    } else {
      return null;
    }
  }

  /// <summary>
  /// Gets the tiles neighboring the given coordinates.
  /// Does <b>NOT</b> include unexisting tiles.
  /// </summary>
  public IEnumerable<MapTileData> Neighbors(HexCoords coords) {
    var neighborCoords = coords.Neighbors();

    foreach (var neighbor in neighborCoords) {
      var tile = GetTileAt(neighbor);

      if (tile != null) {
        yield return tile.Value;
      }
    }
  }

  /// <summary>
  /// Gets the tiles neighboring the given coordinates.
  /// Includes unexisting tiles as null.
  /// </summary>
  public IEnumerable<MapTileData?> NeighborsNullable(HexCoords coords) {
    var neighborCoords = coords.Neighbors();

    foreach (var neighbor in neighborCoords) {
      yield return GetTileAt(neighbor);
    }
  }

  /// <summary>
  /// Gets the tiles neighboring the given coordinates with their directions.
  /// Does <b>NOT</b> include unexisting tiles.
  /// </summary>
  public IEnumerable<(HexDirection, MapTileData)> NeighborsWithDirections(HexCoords coords) {
    var neighborCoords = coords.NeighborsWithDirections();

    foreach (var (direction, neighbor) in neighborCoords) {
      var tile = GetTileAt(neighbor);

      if (tile != null) {
        yield return (direction, tile.Value);
      }
    }
  }

  /// <summary>
  /// Gets the tiles neighboring the given coordinates with their directions.
  /// Includes unexisting tiles as null.
  /// </summary>
  public IEnumerable<(HexDirection, MapTileData?)> NeighborsWithDirectionsNullable(HexCoords coords) {
    var neighborCoords = coords.NeighborsWithDirections();

    foreach (var (direction, neighbor) in neighborCoords) {
      yield return (direction, GetTileAt(neighbor));
    }
  }
}