using System;
using System.Collections.Generic;
using Chickensoft.AutoInject;
using Chickensoft.Introspection;
using Godot;


[Meta(typeof(IAutoConnect), typeof(IAutoNode))]
public partial class GameDataInterface : Node {
  public override void _Notification(int what) => this.Notify(what);
  public static readonly string ScenePath = "uid://wq18lv7molho";

  public static GameDataInterface Instantiate() {
    var scene = ResourceLoader.Load<PackedScene>(ScenePath);
    var instance = scene.Instantiate<GameDataInterface>();
    return instance;
  }

  public override void _Ready() {
    if (MultiplayerManager.IsHost) {
      testValue = new ServerValue<string>("host", this, nameof(testValue));
    } else {
      testValue = new ServerValue<string>("host", this, nameof(testValue));
    }
  }

  public ServerValue<string> testValue = default!;

  [Export] public string TestValue {
    get => testValue.Value;
    set { }
  }
}
