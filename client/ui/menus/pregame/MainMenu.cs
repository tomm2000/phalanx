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
public partial class MainMenu : Control {
  public override void _Notification(int what) => this.Notify(what);
  public static readonly string ScenePath = "uid://dwjcl253px63k";

  private void OnSingleplayerButtonPressed() {
    Main.Instance.SwitchScene(SingleplayerMenu.ScenePath);
  }
  private void OnMultiplayerButtonPressed() {
    Main.Instance.SwitchScene(MultiplayerMenu.ScenePath);
  }
  private void OnSettingsButtonPressed() { }
  private void OnQuitButtonPressed() {
    GetTree().Quit();
  }
}
