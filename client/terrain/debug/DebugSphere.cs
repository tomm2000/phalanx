using System;
using System.Collections.Generic;
using Chickensoft.AutoInject;
using Chickensoft.Introspection;
using Godot;


[Meta(typeof(IAutoConnect), typeof(IAutoNode))]
public partial class DebugSphere : Node3D {
	public override void _Notification(int what) => this.Notify(what);
	public static readonly string ScenePath = "uid://dn2jlnrm6t8g8";
	
	public static DebugSphere Instantiate() {
		var scene = ResourceLoader.Load<PackedScene>(ScenePath);
		var instance = scene.Instantiate<DebugSphere>();
		return instance;
	}
}
