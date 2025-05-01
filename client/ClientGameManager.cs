using System;
using System.Collections.Generic;
using Chickensoft.AutoInject;
using Chickensoft.Collections;
using Chickensoft.Introspection;
using Client.Terrain;
using Client.UI;
using Godot;

namespace Client;

[Meta(typeof(IAutoConnect), typeof(IAutoNode))]
public partial class ClientGameManager : Node,
  IProvide<ClientEventBus>
{
  public override void _Notification(int what) => this.Notify(what);
  public static readonly string ScenePath = "uid://dn86mibpjnhpv";

  ClientEventBus IProvide<ClientEventBus>.Value() => ClientEventBus;

  [Dependency] GameInstance GameInstance => this.DependOn<GameInstance>();

  #region Nodes
  [Node] ClientEventBus ClientEventBus { get; set; } = default!;
  #endregion

  private Node ActiveScene { get; set; } = default!;

  public static ClientGameManager Instantiate() {
    var scene = ResourceLoader.Load<PackedScene>(ScenePath);
    var instance = scene.Instantiate<ClientGameManager>();

    return instance;
  }

  public void OnResolved() {
    GameInstance.GameStageChanged += OnGameStageChanged;

    OnGameStageChanged(GameInstance.currentGameStage, GameInstance.currentGameStage);

    this.Provide();
  }

  private void OnGameStageChanged(GameStage oldStage, GameStage newStage) {
    GD.Print($"Game stage changed from {oldStage} to {newStage}");
    
    switch (newStage) {
      case GameStage.Lobby:
        LoadLobbyScene();
        break;
      case GameStage.Battle:
        SwitchScene(ClientBattleStage.Instantiate());
        break;
      default:
        throw new ArgumentOutOfRangeException(nameof(newStage), newStage, null);
    }
  }

  private void LoadLobbyScene() {
    switch (MultiplayerManager.MultiplayerStatus) {
      case MultiplayerStatus.SinglePlayer:
        SwitchScene(SingleplayerLobbyMenu.ScenePath);
        break;
      case MultiplayerStatus.EnetMultiplayer:
      case MultiplayerStatus.SteamMultiplayer:
        SwitchScene(MultiplayerMenu.ScenePath);
        break;
      case MultiplayerStatus.Disconnected:
        Main.SwitchScene(MainMenu.ScenePath);
        break;
    }
  }

  #region Scene Management
  private void SwitchScene(string path) {
    SwitchScene((PackedScene)ResourceLoader.Load(path));
  }

  private void SwitchScene(PackedScene scene) {
    var instance = scene.Instantiate();
    SwitchScene(instance);
  }

  private void SwitchScene(Node scene) {
    ActiveScene?.QueueFree();
    ActiveScene = scene;
    AddChild(ActiveScene, true);
  }
  #endregion
}
