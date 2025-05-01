using System;
using System.Collections.Generic;
using Chickensoft.AutoInject;
using Chickensoft.Introspection;
using Client;
using Client.UI;
using Godot;
using Tlib.Nodes;

public enum GameStage {
  Lobby,
  Deployment,
  Battle
}


[Meta(typeof(IAutoConnect), typeof(IAutoNode))]
public partial class GameInstance :
  Node,
  IProvide<GameStage>,
  IProvide<GameInstance>,
  IProvide<GameDataInterface>,
  IProvide<ServerToClientInterface>,
  IProvide<ClientToServerInterface>
{
  public override void _Notification(int what) => this.Notify(what);
  public static readonly string ScenePath = "uid://cexnf1ilp6b4b";

  public GameStage currentGameStage { get; private set; } = GameStage.Lobby;
  
  GameStage IProvide<GameStage>.Value() => currentGameStage;
  GameInstance IProvide<GameInstance>.Value() => this;
  GameDataInterface IProvide<GameDataInterface>.Value() => GameDataInterface;
  ServerToClientInterface IProvide<ServerToClientInterface>.Value() => ServerToClientInterface;
  ClientToServerInterface IProvide<ClientToServerInterface>.Value() => ClientToServerInterface;

  public event Action<Player>? SERVER_PlayerReady;
  public event Action<GameStage, GameStage>? GameStageChanged;

  public static GameInstance Instantiate(
    GameStage gameStage = GameStage.Lobby,
    ClientGameManager? client = null,
    ServerGameManager? server = null
  ) {
    var scene = ResourceLoader.Load<PackedScene>(ScenePath);
    var instance = scene.Instantiate<GameInstance>();

    instance.currentGameStage = gameStage;

    client?.AddAsChildTo(instance);
    server?.AddAsChildTo(instance);

    return instance;
  }

  #region Nodes
  [Node] GameDataInterface GameDataInterface { get; set; } = default!;
  [Node] ServerToClientInterface ServerToClientInterface { get; set; } = default!;
  [Node] ClientToServerInterface ClientToServerInterface { get; set; } = default!;
  #endregion

  #region Lifecycle
  public void OnResolved() {
    this.Provide();

    InitializationComplete();
  }

  public override void _ExitTree() {
    SERVER_PlayerReady = null;
    GameStageChanged = null;
  }

  public void ExitGame() {
    MultiplayerManager.Disconnect(MultiplayerDisconnectReason.None);
    Main.SwitchScene(MainMenu.ScenePath);
  }
  #endregion

  #region Client Initialization
  private void InitializationComplete() {
    RpcId(1, nameof(SERVER_OnClientReady));
  }

  [Rpc(
    mode: MultiplayerApi.RpcMode.AnyPeer,
    CallLocal = true,
    TransferMode = MultiplayerPeer.TransferModeEnum.Reliable
  )]
  public void SERVER_OnClientReady() {
    try {
      var player = PlayerManager.RpcSenderPlayer();
      SERVER_PlayerReady?.Invoke(player);
      RpcId(player.PeerId, nameof(CLIENT_LoadGameStage), currentGameStage.Serialize());
    } catch (Exception e) {
      throw new Exception("Failed to get player from RPC sender", e);
    }
  }
  #endregion

  #region Game Stage
  public void SERVER_ChangeGameStage(GameStage gameStage) {
    if (gameStage == currentGameStage) { return; }

    Rpc(nameof(CLIENT_LoadGameStage), gameStage.Serialize());
  }

  [Rpc(
    mode: MultiplayerApi.RpcMode.Authority,
    CallLocal = true,
    TransferMode = MultiplayerPeer.TransferModeEnum.Reliable
  )]
  private void CLIENT_LoadGameStage(byte[] gameStageData) {
    var gameStage = gameStageData.Deserialize<GameStage>();
    if (gameStage == currentGameStage) { return;  }

    var previousGameStage = currentGameStage;
    currentGameStage = gameStage;
    
    GameStageChanged?.Invoke(previousGameStage, gameStage);
  }
  #endregion
}
