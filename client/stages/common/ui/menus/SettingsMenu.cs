using System;
using System.Collections.Generic;
using Chickensoft.AutoInject;
using Chickensoft.Introspection;
using Godot;


[Meta(typeof(IAutoConnect), typeof(IAutoNode))]
public partial class SettingsMenu : Control {
  public override void _Notification(int what) => this.Notify(what);
  public static readonly string ScenePath = "uid://vm8lg46yfki1";

  public static SettingsMenu Instantiate() {
    var scene = ResourceLoader.Load<PackedScene>(ScenePath);
    var instance = scene.Instantiate<SettingsMenu>();
    return instance;
  }

  #region Nodes
  #endregion

  public override void _Process(double delta) {
    if (Input.IsActionJustPressed("ui_cancel")) {
      Exit();
    }
  }

  private void Exit() {
    Main.ToggleSettingsMenu(true);
  }
}
