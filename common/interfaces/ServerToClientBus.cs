using System;
using System.Collections.Generic;
using Chickensoft.AutoInject;
using Chickensoft.Introspection;
using Godot;


[Meta(typeof(IAutoConnect), typeof(IAutoNode))]
public partial class ServerToClientBus : Node {
	public override void _Notification(int what) => this.Notify(what);
	public static readonly string ScenePath = "uid://_UID_";
	
	public static ServerToClientBus Instantiate() {
		var scene = ResourceLoader.Load<PackedScene>(ScenePath);
		var instance = scene.Instantiate<ServerToClientBus>();
		return instance;
	}

	#region Nodes
	#endregion

	public void OnResolved()
	{
	}

	public override void _Ready() {
	}
}
