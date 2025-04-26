using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using Chickensoft.GodotNodeInterfaces;
using Chickensoft.AutoInject;
using Chickensoft.Introspection;
using RandomNameGeneratorLibrary;
using MessagePack.Resolvers;
using GodotSteam;


public enum MultiplayerStatus {
  Disconnected,
  SinglePlayer,
  Multiplayer,
}

public partial class MultiplayerManager : Node {
  #region Properties
  private static MultiplayerManager? _instance;
  public static MultiplayerManager Instance {
    get => _instance ?? throw new Exception("MultiplayerService instance not found");
    private set => _instance = value;
  }

  public static MultiplayerStatus MultiplayerStatus { get; private set; }
  #endregion

  #region Accessors
  public static int PeerId => Instance.Multiplayer.GetUniqueId();
  public static bool IsServer => PeerId == 1;
  public static Func<int> RpcSenderId => Instance.Multiplayer.GetRemoteSenderId;
  public static MultiplayerPeer Peer => Instance.Multiplayer.MultiplayerPeer;
  public static MultiplayerApi MultiplayerApi => Instance.Multiplayer;
  #endregion

  public override void _Ready() {
    Instance = this;

    Instance.Multiplayer.PeerDisconnected += Instance.SERVER_PeerDisconnected;

    Instance.Multiplayer.ConnectedToServer += Instance.CLIENT_ConnectedToServer;
    Instance.Multiplayer.ServerDisconnected += Instance.CLIENT_DisconnectedFromServer;
  }

  #region Initialization
  /// <summary>
  /// Initializes the multiplayer service.
  /// </summary>
  private static void Initialize(MultiplayerPeer peer) {
    if (MultiplayerStatus != MultiplayerStatus.Disconnected) {
      Disconnect();
    }

    if (peer is OfflineMultiplayerPeer) {
      MultiplayerStatus = MultiplayerStatus.SinglePlayer;
    } else {
      MultiplayerStatus = MultiplayerStatus.Multiplayer;
    }

    Instance.Multiplayer.MultiplayerPeer = peer;

    GD.PushWarning($"----------- <multiplayer initialized {PeerId}> -----------");

    // NOTE: this function will only run on a server
    Instance.SERVER_ServerCreated();
  }

  /// <summary>
  /// Disconnects from the current server. <br/>
  /// Resets the player manager and closes the peer.
  /// </summary>
  public static void Disconnect() {
    PlayerManager.Reset();

    Peer?.Close();
    Instance.Multiplayer.MultiplayerPeer = null;

    MultiplayerStatus = MultiplayerStatus.Disconnected;

    GD.PushWarning($"----------- <multiplayer disconnected> -----------");
    Disconnected?.Invoke();
  }

  public static Action? Disconnected;
  #endregion

  #region Server
  private void SERVER_ClientConnected(ConnectionResult result, long peerId) {
    RpcId(peerId, nameof(CLIENT_ConnectionResult), result.pack());

    if (result.Result == ConnectionResultType.Failure) {
      // disconnect the peer
      Peer.DisconnectPeer((int) peerId);
    } else {
      // syncronize the client with the server
      PlayerManager.SERVER_SynchronizeClient(peerId);
    }
  }

  public static Action? SERVER_ServerReady;
  /// <summary>
  /// Called by the godot multiplayer api when the server is created.
  /// </summary>
  private void SERVER_ServerCreated() {
    if (!IsServer) return;
    
    if (Steam.IsSteamRunning()) {
      var steamId = Steam.GetSteamID();
      GD.PushWarning($"<multiplayer> Client connected with steamId: {steamId}");

      PlayerManager.SERVER_SteamPlayerConnected(steamId, 1);
    } else {
      var name = ClientData.Username;
      PlayerManager.SERVER_EnetPlayerConnected(name, 1);
    }

    SERVER_ServerReady?.Invoke();
  }

  /// <summary>
  /// Called by the godot multiplayer api when a client disconnects from the server.
  /// </summary>
  private void SERVER_PeerDisconnected(long id) {
    if (!IsServer) return;

    PlayerManager.SERVER_PlayerDisconnected(id);
  }

  public static void SERVER_DisconnectPeer(long id) {
    if (!IsServer) return;

    Peer.DisconnectPeer((int) id);
  }
  #endregion

  #region Client
  /// <summary>
  /// Called by the godot multiplayer api when the client disconnects from the server.
  /// </summary>
  private void CLIENT_DisconnectedFromServer() {
    if (IsServer) return;

    Disconnect();
  }

  /// <summary>
  /// Called by the godot multiplayer api when the client connects to the server.
  /// </summary>
  private void CLIENT_ConnectedToServer() {
    if (IsServer) return;

    if (Steam.IsSteamRunning()) {
      var steamId = ClientData.SteamId!.Value;
      var name = ClientData.Username;

      // TODO: Proper steam joining system
      GD.PushWarning($"<multiplayer> Client connected with steamId: {steamId} and name: {name}");

      RpcId(1, nameof(SERVER_SteamClientConnected), steamId, name);
    } else {
      var name = ClientData.Username;
      GD.PushWarning($"<multiplayer> Client connected with name: {name}");
      RpcId(1, nameof(SERVER_EnetClientConnected), name);
    }
  }

  public static Action<ConnectionResult>? CLIENT_OnConnectionResult;
  /// <summary>
  /// Called via RPC by the server to notify the client of the connection result.
  /// </summary>
  [Rpc(
    mode: MultiplayerApi.RpcMode.Authority,
    CallLocal = true,
    TransferMode = MultiplayerPeer.TransferModeEnum.Reliable
  )]
  private void CLIENT_ConnectionResult(byte[] result) {
    if (IsServer) return;

    var connectionResult = ConnectionResult.unpack(result);

    // Logging.MultiplayerLog($"Connection result: {connectionResult.Result}");

    CLIENT_OnConnectionResult?.Invoke(connectionResult);
  }
  //============================================================================
  #endregion
}