using System;
using System.Collections.Generic;
using Chickensoft.AutoInject;
using Chickensoft.Introspection;
using Godot;


[Meta(typeof(IAutoConnect), typeof(IAutoNode))]
public partial class ServerManager : Node {
	public override void _Notification(int what) => this.Notify(what);
	public static readonly string ScenePath = "uid://cw1g3y53f7ocr";

  [Dependency] private GameInstance GameInstance => this.DependOn<GameInstance>();
	
	public static ServerManager Instantiate() {
    var scene = ResourceLoader.Load<PackedScene>(ScenePath);
    var instance = scene.Instantiate<ServerManager>();
    return instance;
  }

  #region Nodes
  #endregion
  
	public void OnResolved() {
  }
}
