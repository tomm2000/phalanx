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
  IProvide<GameInstance>,
  IProvide<PlayerManager>,
  IProvide<SharedDataBase> {
  public override void _Notification(int what) => this.Notify(what);
  public static readonly string ScenePath = "uid://cexnf1ilp6b4b";

  GameInstance IProvide<GameInstance>.Value() => this;
  PlayerManager IProvide<PlayerManager>.Value() => _playerManager;
  SharedDataBase IProvide<SharedDataBase>.Value() => _sharedDataBase;

  #region Nodes
  [Node] private PlayerManager _playerManager { get; set; } = default!;
  [Node] private SharedDataBase _sharedDataBase { get; set; } = default!;
  #endregion

  public event Action<Player>? SERVER_PlayerReady;
  public event Action<GameStage, GameStage>? GameStageChanged;

  public static GameInstance Instantiate(bool withServer) {
    var scene = ResourceLoader.Load<PackedScene>(ScenePath);
    var instance = scene.Instantiate<GameInstance>();

    if (withServer) {
      var server = ServerManager.Instantiate();
      instance.AddChild(server);
    }

    return instance;
  }

  public void AttachClient(Player player) {
    var clientInterface = ClientInterface.Instantiate(player);

    ClientController clientController = player.PlayerType switch {
      PlayerType.Human => PlayerClientController.Instantiate(),
      PlayerType.Bot => throw new NotImplementedException("Bot client interface not implemented"),
      _ => throw new NotImplementedException($"Player type {player.PlayerType} not implemented"),
    };

    AddChild(clientInterface, true);

    clientInterface.AttachClientController(clientController);
  }

  #region Nodes
  [Node] public PlayerManager PlayerManager { get; private set; } = default!;
  #endregion

  #region Lifecycle
  public void OnResolved() {
    this.Provide();

    RpcId(1, nameof(SERVER_InitializePeer));
  }

  [Rpc(
    mode: MultiplayerApi.RpcMode.AnyPeer,
    CallLocal = true,
    TransferMode = MultiplayerPeer.TransferModeEnum.Reliable
  )]
  private void SERVER_InitializePeer() {
    if (!MultiplayerManager.IsHost) return;

    var peerId = MultiplayerManager.RpcSenderId();
    SERVER_PeerInitialized?.Invoke(peerId);
  }

  public event Action<long>? SERVER_PeerInitialized;
  #endregion

  #region Game Stage
  #endregion
}
