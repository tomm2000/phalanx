using System;
using System.Threading.Tasks;
using Godot;
using Chickensoft.AutoInject;
using Chickensoft.Introspection;
using System.Collections.Generic;
using Tlib;
using System.Linq;



[Meta(typeof(IAutoConnect), typeof(IAutoNode))]
public partial class MainMenu : Control {
  public override void _Notification(int what) => this.Notify(what);
  public static readonly string ScenePath = "uid://dwjcl253px63k";

  [Dependency] public PlayerClientController PlayerClientController => this.DependOn<PlayerClientController>();

  private void OnSingleplayerButtonPressed() {
    // TODO: switch to singleplayer menu
    MultiplayerManager.HostSinglePlayer();
  }
  
  private void OnMultiplayerButtonPressed() {
    PlayerClientController.SwitchScene(MultiplayerMenu.ScenePath);
  }

  private void OnSettingsButtonPressed() {
    Main.ToggleSettingsMenu();
  }

  private void OnQuitButtonPressed() {
    GetTree().Quit();
  }
}
