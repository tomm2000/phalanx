using System;
using System.Collections.Generic;
using Chickensoft.AutoInject;
using Chickensoft.Introspection;

using Godot;



[Meta(typeof(IAutoConnect), typeof(IAutoNode))]
public partial class PlayerClientController :
  ClientController,
  IProvide<PlayerClientController>
{
	public override void _Notification(int what) => this.Notify(what);
	public static readonly string ScenePath = "uid://nss3qj5556mk";

  PlayerClientController IProvide<PlayerClientController>.Value() => this;
  
  #region Nodes
  [Dependency] ClientInterface ClientInterface => this.DependOn<ClientInterface>();
  #endregion

  public static PlayerClientController Instantiate() {
    var scene = ResourceLoader.Load<PackedScene>(ScenePath);
    var instance = scene.Instantiate<PlayerClientController>();
    return instance;
  }

	private Node ActiveScene { get; set; } = default!;

  public override void _Ready() {
    base._Ready();
    this.Provide();

    ActiveScene = GetNode<MainMenu>("%MainMenu");
  }

  public override void OnResolved() {
    Logger.Dev("PlayerClientController resolved.");
    base.OnResolved();
  }

  public override void _ExitTree() {
    base._ExitTree();
  }

  protected override void OnGameStageChanged(GameStage oldStage, GameStage newStage) {
    Logger.Info($"Game stage changed from {oldStage} to {newStage}");

    switch (newStage) {
      case GameStage.Disconnected:
        break;
      case GameStage.Lobby:
        LoadLobbyScene();
        break;
      case GameStage.Battle:
        SwitchScene(ClientBattleStage.Instantiate());
        break;
      default:
        Logger.Warn($"No scene handling implemented for game stage: {newStage}");
        break;
    }
  }

  private void LoadLobbyScene() {
    Logger.Debug($"Loading lobby scene for multiplayer status: {MultiplayerManager.MultiplayerStatus}");
    
    switch (MultiplayerManager.MultiplayerStatus) {
      case MultiplayerStatus.SinglePlayer:
        SwitchScene(SingleplayerLobbyMenu.ScenePath);
        break;
      case MultiplayerStatus.EnetMultiplayer:
      case MultiplayerStatus.SteamMultiplayer:
        SwitchScene(MultiplayerLobbyMenu.ScenePath);
        break;
      case MultiplayerStatus.Disconnected:
        SwitchScene(MainMenu.ScenePath);
        break;
    }
  }

  #region Scene Management
  public void SwitchScene(string path) {
    SwitchScene((PackedScene)ResourceLoader.Load(path));
  }

  public void SwitchScene(PackedScene scene) {
    var instance = scene.Instantiate();
    SwitchScene(instance);
  }

  public void SwitchScene(Node scene) {
    ActiveScene?.QueueFree();
    ActiveScene = scene;
    AddChild(ActiveScene, true);
  }
  #endregion
}
