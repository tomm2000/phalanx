using System;
using System.Collections.Generic;
using Chickensoft.AutoInject;
using Chickensoft.Introspection;
using Godot;


[Meta(typeof(IAutoConnect), typeof(IAutoNode))]
public partial class ClientBattleStage : Node {
	public override void _Notification(int what) => this.Notify(what);
	public static readonly string ScenePath = "uid://g18c7hr0hg8v";
  
  [Dependency] private ScenarioManager ScenarioManager => this.DependOn<ScenarioManager>();
	
	public static ClientBattleStage Instantiate() {
    var scene = ResourceLoader.Load<PackedScene>(ScenePath);
    var instance = scene.Instantiate<ClientBattleStage>();
    return instance;
  }

  #region Nodes
  [Node] StandardTerrain Terrain { get; set; } = default!;
  #endregion

  public void OnResolved() {
    // var map = MapManager.SelectedMap.Value ?? throw new Exception("Started without a map selected");
    var map = ScenarioManager.GetSelectedMap();

    Terrain.GenerateTerrain(map!);
  }

	public override void _Ready() {
	}
}
