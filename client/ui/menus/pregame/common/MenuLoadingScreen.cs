using System;
using System.Collections.Generic;
using Chickensoft.AutoInject;
using Chickensoft.Introspection;
using Godot;

namespace Client.UI;

[Meta(typeof(IAutoConnect), typeof(IAutoNode))]
public partial class MenuLoadingScreen : Control {
  public override void _Notification(int what) => this.Notify(what);
  public static readonly string ScenePath = "uid://ds17brhc3vm3e";

  public static MenuLoadingScreen Instantiate(
    string text,
    string buttonText = "OK",
    float timeout = 5,
    string nextScene = "mainmenu"
  ) {
    var scene = ResourceLoader.Load<PackedScene>(ScenePath);
    var instance = scene.Instantiate<MenuLoadingScreen>();
    instance.text = text;
    instance.timeout = timeout;
    instance.nextScene = nextScene == "mainmenu" ? MainMenu.ScenePath : nextScene;
    instance.buttonText = buttonText;

    return instance;
  }

  private string text = string.Empty;
  private string buttonText = "OK";
  private float timeout = 5f;
  private string nextScene = string.Empty;
  

  #region Nodes
  [Node] private Label LoadingText { get; set; } = default!;
  [Node] private Timer TimeoutTimer { get; set; } = default!;
  [Node] private Button OkButton { get; set; } = default!;
  #endregion

  public override void _Ready() {
    LoadingText.Text = text;
    OkButton.Text = buttonText;

    if (timeout > 0) {
      TimeoutTimer.WaitTime = timeout;
      TimeoutTimer.Start();
    } else {
      TimeoutTimer.Stop();
    }
  }

  private void GotoNextScene() {
    Main.SwitchScene(nextScene);

    GD.Print("Switching to scene: ", nextScene);

    if (nextScene == MainMenu.ScenePath) {
      Main.Reset();
    }
  }
}
