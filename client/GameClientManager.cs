using System;
using System.Collections.Generic;
using Chickensoft.AutoInject;
using Chickensoft.Collections;
using Chickensoft.Introspection;
using Client.Terrain;
using Godot;


[Meta(typeof(IAutoConnect), typeof(IAutoNode))]
public partial class GameClientManager : Node {
	public override void _Notification(int what) => this.Notify(what);
	public static readonly string ScenePath = "uid://dn86mibpjnhpv";

  #region Nodes
  #endregion

  public MapData Map { get; private set; } = default!;

  public static GameClientManager Instantiate(
    MapData map
  ) {
    var scene = ResourceLoader.Load<PackedScene>(ScenePath);
    var instance = scene.Instantiate<GameClientManager>();

    instance.Map = map;

    return instance;
  }

  public override void _Ready() {
    var terrain = StandardTerrain.Instantiate();
    AddChild(terrain);

    terrain.GenerateTerrain(Map);
  }
}
