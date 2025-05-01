using System;
using System.Collections.Generic;
using Chickensoft.AutoInject;
using Chickensoft.Introspection;
using Godot;


[Meta(typeof(IAutoConnect), typeof(IAutoNode))]
public partial class ServerGameManager : Node {
	public override void _Notification(int what) => this.Notify(what);
	public static readonly string ScenePath = "uid://cw1g3y53f7ocr";

  public GameInstance GameInstance { get; private set; } = default!;
	
	public static ServerGameManager Instantiate() {
		var scene = ResourceLoader.Load<PackedScene>(ScenePath);
		var instance = scene.Instantiate<ServerGameManager>();
		return instance;
	}

	#region Nodes
	#endregion
	public override void _Ready() {
    GameInstance = GetParent<GameInstance>();
	}
}
