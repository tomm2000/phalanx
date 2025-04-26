using System;
using System.Threading.Tasks;
using Godot;
using Chickensoft.AutoInject;
using Chickensoft.Introspection;
using Tlib.Nodes;
using System.Collections.Generic;
using Tlib;
using System.Linq;

[Meta(typeof(IAutoConnect), typeof(IAutoNode))]
public partial class SingleplayerMenu : Control {
  public override void _Notification(int what) => this.Notify(what);
  public static readonly string ScenePath = "uid://dukh8gnwwqll6";

  private void OnQuitToMainMenuButtonPressed() {
    Main.Instance.SwitchScene(MainMenu.ScenePath);
  }
}
