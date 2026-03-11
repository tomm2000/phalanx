using System;
using System.Collections.Generic;
using Chickensoft.AutoInject;
using Chickensoft.Introspection;


using FluentResults;
using Godot;
using Tlib.NodeExt;

public enum GameStage {
  Lobby,
  Deployment,
  Battle
}


[Meta(typeof(IAutoConnect), typeof(IAutoNode))]
public partial class GameInstance :
  Node,
  IProvide<GameInstance>,
  IProvide<ClientManager>,
  IProvide<LobbyManager>,
  IProvide<ScenarioManager>,
  IProvide<NetStateManager>
{
  public override void _Notification(int what) => this.Notify(what);
  public static readonly string ScenePath = "uid://cexnf1ilp6b4b";

  GameInstance IProvide<GameInstance>.Value() => this;
  ClientManager IProvide<ClientManager>.Value() => ClientManager;
  LobbyManager IProvide<LobbyManager>.Value() => LobbyManager;
  ScenarioManager IProvide<ScenarioManager>.Value() => ScenarioManager;
  NetStateManager IProvide<NetStateManager>.Value() => NetStateManager;

  #region Nodes
  [Node] public ClientManager ClientManager { get; private set; } = default!;
  [Node] public LobbyManager LobbyManager { get; private set; } = default!;
  [Node] public ScenarioManager ScenarioManager { get; private set; } = default!;
  [Node] public NetStateManager NetStateManager { get; private set; } = default!;
  #endregion

  #region Properties
  private bool isFirstFrameProcessed = false;
  #endregion

  #region Events
  #endregion

  public static GameInstance Instantiate(bool withServer) {
    var scene = ResourceLoader.Load<PackedScene>(ScenePath);
    var instance = scene.Instantiate<GameInstance>();

    if (withServer) {
      var server = ServerManager.Instantiate();
      instance.AddChild(server);
    }

    return instance;
  }

  // TODO: create a save function that saves the current game instance state to a GameStateData object,
  // and a load function that takes a GameStateData object and sets the game instance state accordingly.

  public override void _Ready() {
    this.Provide();

  }

  #region Lifecycle
  public void OnResolved() {
    RpcId(1, nameof(SERVER_InitializePeer));
  }

  private event Action? FirstFrameProcessed;

  public override void _Process(double delta) {
    if (!isFirstFrameProcessed) {
      isFirstFrameProcessed = true;
      FirstFrameProcessed?.Invoke();
      return;
    }
  }

  public void OnFirstFrameProcessedSafe(Action callback) {
    if (isFirstFrameProcessed) {
      callback();
    } else {
      FirstFrameProcessed += callback;
    }
  }

  [Rpc(
    mode: MultiplayerApi.RpcMode.AnyPeer,
    CallLocal = true,
    TransferMode = MultiplayerPeer.TransferModeEnum.Reliable
  )]
  private void SERVER_InitializePeer() {
    if (!MultiplayerManager.IsHost) return;

    var peerId = MultiplayerManager.RpcSenderId();
    SERVER_SyncPeer?.Invoke(peerId);
    SERVER_SyncPeerFinished?.Invoke(peerId);
  }

  public event Action<PeerID>? SERVER_SyncPeer;
  public event Action<PeerID>? SERVER_SyncPeerFinished;
  #endregion


  #region Client Interfaces
  private IEnumerable<ClientInterface> ClientInterfaces => this.GetChildren<ClientInterface>();

  public ClientInterface AttachClient(Client client) {
    var clientInterface = ClientInterface.Instantiate(client);

    AddChild(clientInterface, forceReadableName: true);

    return clientInterface;
  }

  public void DetachClient(Client client) {
    var clientInterface = GetClientInterface(client.UID);
    if (clientInterface.IsSuccess) {
      clientInterface.Value.QueueFree();
    }
  }

  public Result<ClientInterface> GetClientInterface(string playerUID) {
    foreach (var clientInterface in ClientInterfaces) {
      if (clientInterface.Client.UID == playerUID) {
        return clientInterface;
      }
    }
    return Result.Fail($"No client interface found for player UID {playerUID}");
  }
  #endregion

  #region Game Stage
  #endregion
}
