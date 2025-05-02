using System;
using System.Collections.Generic;
using Chickensoft.AutoInject;
using Chickensoft.Introspection;
using Client.Terrain;
using Godot;


[Meta(typeof(IAutoConnect), typeof(IAutoNode))]
public partial class ClientBattleStage : Node {
	public override void _Notification(int what) => this.Notify(what);
	public static readonly string ScenePath = "uid://g18c7hr0hg8v";
  
  [Dependency] private SharedDataBase SharedDataBase => this.DependOn<SharedDataBase>();
	
	public static ClientBattleStage Instantiate() {
    var scene = ResourceLoader.Load<PackedScene>(ScenePath);
    var instance = scene.Instantiate<ClientBattleStage>();
    return instance;
  }

  #region Nodes
  [Node] StandardTerrain Terrain { get; set; } = default!;
  #endregion

  public void OnResolved() {
    var map = SharedDataBase.SelectedMap ?? throw new Exception("Started without a map selected");

    Terrain.GenerateTerrain(map!);
  }

	public override void _Ready() {
	}
}
