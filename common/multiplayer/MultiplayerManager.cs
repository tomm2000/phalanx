using Godot;
using Steamworks;
using Steamworks.Data;
using System;

public enum MultiplayerStatus {
  Disconnected,
  SinglePlayer,
  Multiplayer,
}

public enum MultiplayerResetReason {
  None,
  Disconnect,
  ServerDisconnected,
  Error,
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
  public static bool IsHost => PeerId == 1;
  public static Func<int> RpcSenderId => Instance.Multiplayer.GetRemoteSenderId;
  public static MultiplayerPeer Peer => Instance.Multiplayer.MultiplayerPeer;
  public static MultiplayerApi MultiplayerApi => Instance.Multiplayer;
  #endregion

  public override void _Ready() {
    Instance = this;

    Instance.Multiplayer.PeerDisconnected += Instance.SERVER_OnPeerDisconnected;
    Instance.Multiplayer.ConnectedToServer += Instance.CLIENT_OnConnectedToServer;
    Instance.Multiplayer.ServerDisconnected += Instance.CLIENT_OnDisconnectedFromServer;

    SteamMatchmaking.OnLobbyEntered += OnSteamLobbyEntered;
    SteamMatchmaking.OnLobbyCreated += OnSteamLobbyCreated;

    SteamFriends.OnGameLobbyJoinRequested += OnGameLobbyJoinRequested;
  }

  /// <summary>
  /// Called when the user tries to join a lobby from their friends list or from an invite.
  /// https://partner.steamgames.com/doc/api/ISteamFriends#GameLobbyJoinRequested_t
  /// </summary>
  private void OnGameLobbyJoinRequested(Lobby lobby, SteamId id) {
    ConnectSteam(lobby);
  }

  #region Initialization
  /// <summary>
  /// Initializes the multiplayer service.
  /// </summary>
  private static void Initialize(MultiplayerPeer peer) {
    if (MultiplayerStatus != MultiplayerStatus.Disconnected) {
      Reset(MultiplayerResetReason.Error);
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
  #endregion

  #region Disconnect
  /// <summary>
  /// Disconnects from the current server. <br/>
  /// Resets the player manager and closes the peer.
  /// </summary>
  public static void Reset(
    MultiplayerResetReason reason
  ) {
    PlayerManager.Reset();

    Peer.DisconnectPeer(1);
    Instance.Multiplayer.MultiplayerPeer = null;

    MultiplayerStatus = MultiplayerStatus.Disconnected;

    LeaveSteamLobby();

    // FIXME: this is jank
    Main.RestartSteam();

    Disconnected?.Invoke(reason);
  }

  public static Action<MultiplayerResetReason>? Disconnected;

  /// <summary>
  /// SERVER: Called by the godot multiplayer api when a client disconnects from the server.
  /// </summary>
  private void SERVER_OnPeerDisconnected(long id) {
    GD.PushWarning($"<multiplayer> Peer disconnected with id: {id}");

    if (id == 1) {
      GD.PushWarning("<multiplayer> Server disconnected from client.");
      Reset(MultiplayerResetReason.ServerDisconnected);
      return;
    }

    if (!IsHost) return;

    PlayerManager.SERVER_PlayerDisconnected(id);
  }

  /// <summary>
  /// SERVER: Disconnects a peer from the server.
  /// </summary>
  public static void SERVER_DisconnectPeer(long id) {
    if (!IsHost) return;

    Peer.DisconnectPeer((int)id);
  }

  /// <summary>
  /// CLIENT: Called by the godot multiplayer api when the client disconnects from the server.
  /// </summary>
  private void CLIENT_OnDisconnectedFromServer() {
    GD.PushWarning($"<multiplayer> Client disconnected from server with id: {PeerId}");

    if (IsHost) return;

    Reset(MultiplayerResetReason.Disconnect);
  }
  #endregion

  /// <summary>
  /// Called by the mpAPI when the server is created.
  /// </summary>
  private void SERVER_ServerCreated() {
    if (!IsHost) return;

    if (SteamClient.IsValid) {
      var steamId = SteamClient.SteamId;
      var name = ClientData.Username;
      PlayerManager.SERVER_SteamPlayerConnected(steamId, 1, name);
    } else {
      var name = ClientData.Username;
      PlayerManager.SERVER_EnetPlayerConnected(name, 1);
    }

    SERVER_ServerReady?.Invoke();
  }
  public static Action? SERVER_ServerReady;

  #region Client Connection
  // --------------------------------------------------------------------------
  // Client Connection
  // 1. The client connects to the server, and the mpAPI calls CLIENT_OnConnectedToServer.
  // 2. The client sends data about iself to the server via RPCs.
  // --------------------------------------------------------------------------
  /// <summary>
  /// Called by the mpAPI when the client connects to the server.
  /// </summary>
  private void CLIENT_OnConnectedToServer() {
    if (IsHost) return;

    if (SteamClient.IsValid) {
      var steamId = ClientData.SteamId!.Value;
      var name = ClientData.Username;
      RpcId(1, nameof(SERVER_SteamClientConnected), (ulong)steamId, name);
    } else {
      var name = ClientData.Username;
      RpcId(1, nameof(SERVER_EnetClientConnected), name);
    }
  }

  // --------------------------------------------------------------------------
  // 3. The server receives the RPC based on the connection type.
  // 4. The player manager handles the new player and returns a connection result.
  // ---------------------------------------------------------------------------
  /// <summary>
  /// Called by the client to notify the server of a new connection.
  /// </summary>
  [Rpc(
    mode: MultiplayerApi.RpcMode.AnyPeer,
    CallLocal = true,
    TransferMode = MultiplayerPeer.TransferModeEnum.Reliable
  )]
  private void SERVER_SteamClientConnected(ulong steamId, string name) {
    if (!IsHost) return;

    var peerId = Multiplayer.GetRemoteSenderId();
    var result = PlayerManager.SERVER_SteamPlayerConnected(steamId, peerId, name);

    SERVER_ClientConnected(result, peerId);
  }

  /// <summary>
  /// Called by the client to notify the server of a new connection.
  /// </summary>
  [Rpc(
    mode: MultiplayerApi.RpcMode.AnyPeer,
    CallLocal = true,
    TransferMode = MultiplayerPeer.TransferModeEnum.Reliable
  )]
  private void SERVER_EnetClientConnected(string name) {
    if (!IsHost) return;

    var peerId = Multiplayer.GetRemoteSenderId();
    var result = PlayerManager.SERVER_EnetPlayerConnected(name, peerId);

    SERVER_ClientConnected(result, peerId);
  }

  // --------------------------------------------------------------------------
  // 5. The server notifies the client of the connection result, if the result is failure the peer is disconnected.
  // ---------------------------------------------------------------------------
  /// <summary>
  /// SERVER: Called by mpAPI SERVER_SteamClientConnected and SERVER_EnetClientConnected.
  /// Notifies the client of the connection result.
  /// </summary>
  private void SERVER_ClientConnected(ConnectionResult result, long peerId) {
    RpcId(peerId, nameof(CLIENT_ConnectionResult), result.Serialize());

    if (result.Result == ConnectionResultType.Failure) {
      // disconnect the peer
      Peer.DisconnectPeer((int)peerId);
    } else {
      // syncronize the client with the server
      PlayerManager.SERVER_SynchronizeClient(peerId);
    }
  }

  // --------------------------------------------------------------------------
  // 6. The client receives the connection result and handles it.
  // ---------------------------------------------------------------------------
  /// <summary>
  /// Called via RPC by the server to notify the client of the connection result.
  /// </summary>
  [Rpc(
    mode: MultiplayerApi.RpcMode.Authority,
    CallLocal = true,
    TransferMode = MultiplayerPeer.TransferModeEnum.Reliable
  )]
  private void CLIENT_ConnectionResult(byte[] result) {
    if (IsHost) return;

    GD.PushWarning($"<multiplayer> Client connection result: {result}");

    var connectionResult = result.Deserialize<ConnectionResult>();

    CLIENT_OnConnectionResult?.Invoke(connectionResult);
  }
  public static Action<ConnectionResult>? CLIENT_OnConnectionResult;
  #endregion
}