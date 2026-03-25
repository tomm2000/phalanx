using Godot;
using System;

public partial class MapGenTest : Node {
  // Called when the node enters the scene tree for the first time.
  public override void _Ready() {
    var map = DevMap.GenerateMap(width: 19, height: 19, seed: 1);

    Logger.Dev($"Map generated with ID: {map.mapId}");

    var json = map.ToJson();

    // save to res://dev/map.json
    using var file = FileAccess.Open("res://dev/map.json", FileAccess.ModeFlags.Write);
    file.StoreString(json.ToJsonString());

    // close game
    GetTree().Quit();
  }
}
