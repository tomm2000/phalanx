using System;
using Client;
using Godot;
using Steam;
using Steamworks;
using Steamworks.Data;

public enum MultiplayerStatus {
  Disconnected,
  SinglePlayer,
  EnetMultiplayer,
  SteamMultiplayer,
}

public enum MultiplayerDisconnectReason {
  None,
  ServerDisconnected,
  Error,
}

public enum LobbyType {
  Private,
  FriendsOnly,
  Public,
  // Invisible,
  // PrivateUnique
}

public partial class MultiplayerManager : Node {
  private static MultiplayerManager Instance = default!;

  public override void _Ready() {
    Instance = this;

    SteamMatchmaking.OnLobbyEntered += OnSteamLobbyEntered;
    
    /// <summary>
    /// Called when the user tries to join a lobby from their friends list or from an invite.
    /// https://partner.steamgames.com/doc/api/ISteamFriends#GameLobbyJoinRequested_t
    /// </summary>
    SteamFriends.OnGameLobbyJoinRequested += (lobby, id) => JoinSteam(lobby);

    Instance.Multiplayer.ConnectedToServer += () => { CLIENT_ConnectedToServer?.Invoke(); };
    Instance.Multiplayer.PeerDisconnected += OnPeerDisconnected;

    // NOTE: this might never be called
    Instance.Multiplayer.ServerDisconnected += OnDisconnectedFromServer;
  }

  #region Public interface
  public static MultiplayerPeer Peer => Instance.Multiplayer.MultiplayerPeer;
  public static int PeerId => Instance.Multiplayer.GetUniqueId();
  public static bool IsHost => PeerId == 1;
  public static Func<int> RpcSenderId => Instance.Multiplayer.GetRemoteSenderId;
  public static Lobby? ActiveSteamLobby { get; private set; } = null;
  public static MultiplayerStatus MultiplayerStatus { get; private set; } = MultiplayerStatus.Disconnected;

  public async static void HostSteam(int maxPlayers = 10, LobbyType lobbyType = LobbyType.Public) {
    var newLobby = await SteamMatchmaking.CreateLobbyAsync(maxPlayers);

    if (newLobby is not Lobby lobby) {
      GD.PushError("<steam> Failed to create lobby");
      return;
    }

    lobby.SetData("hostname", ClientData.Username);
    lobby.SetData("game", "phalanx");
    lobby.SetJoinable(true);

    switch (lobbyType) {
      case LobbyType.Private:
        lobby.SetPrivate();
        break;
      case LobbyType.FriendsOnly:
        lobby.SetFriendsOnly();
        break;
      case LobbyType.Public:
        lobby.SetPublic();
        break;
    }
  }

  public static void HostEnet(int port, int maxConnections = 10) {
    var peer = new ENetMultiplayerPeer();
    var error = peer.CreateServer(port, maxConnections);

    if (error == Error.Ok) {
      Initialize(peer);
      SERVER_CreatedServer?.Invoke();
    } else {
      GD.PushError($"<enet> Failed to create server: {error}");
    }
  }

  public static void HostSinglePlayer() {
    var peer = new OfflineMultiplayerPeer();
    Initialize(peer);
    SERVER_CreatedServer?.Invoke();
  }

  public static void JoinSteam(Lobby lobby) {
    lobby.Join();
  }

  public static void JoinEnet(string address, int port) {
    var peer = new ENetMultiplayerPeer();
    var error = peer.CreateClient(address, port);

    if (error == Error.Ok) {
      Initialize(peer);
    } else {
      GD.PushError($"<enet> Failed to create client: {error}");
    }
  }

  public static void KickPeer(long peerId) {
    if (!IsHost) throw new InvalidOperationException($"[{nameof(KickPeer)}] Only the host can call this method.");

    Peer.DisconnectPeer((int) peerId);
  }
  
  public static event Action<MultiplayerDisconnectReason>? CLIENT_Disconnected;
  public static event Action? CLIENT_ConnectedToServer;
  public static event Action? SERVER_CreatedServer;
  public static event Action<long>? SERVER_PlayerDisconnected;
  #endregion

  #region Initialization
  private static void Initialize(MultiplayerPeer peer) {
    if (MultiplayerStatus != MultiplayerStatus.Disconnected) {
      GD.PushWarning("MultiplayerManager: Resetting multiplayer connection");
      Disconnect(MultiplayerDisconnectReason.Error);
    }

    MultiplayerStatus = peer switch {
      SteamMultiplayerPeer => MultiplayerStatus.SteamMultiplayer,
      ENetMultiplayerPeer => MultiplayerStatus.EnetMultiplayer,
      OfflineMultiplayerPeer => MultiplayerStatus.SinglePlayer,
      _ => throw new ArgumentException("Invalid peer type", nameof(peer)),
    };
    Instance.Multiplayer.MultiplayerPeer = peer;

    GD.PushWarning($"----------- <multiplayer initialized {PeerId}> -----------");
  }

  public static void Disconnect(MultiplayerDisconnectReason reason) {
    MultiplayerStatus = MultiplayerStatus.Disconnected;

    if (ActiveSteamLobby is Lobby lobby) {
      lobby.Leave();
      ActiveSteamLobby = null;
    }

    if (Peer != null) {
      if (IsHost) {
        Peer.Close();
      } else {
        try { Peer.DisconnectPeer(1); } catch (Exception) { }
      }
    }
    
    Instance.Multiplayer.MultiplayerPeer = null;
    
    CLIENT_Disconnected?.Invoke(reason);
  }
  #endregion

  #region Callbacks
  private static void OnSteamLobbyEntered(Lobby lobby) {
    ActiveSteamLobby = lobby;
    var peer = new SteamMultiplayerPeer();

    Error error;
    if (lobby.Owner.Id == ClientData.SteamId) {
      error = peer.CreateHost(ClientData.SteamId!.Value);
    } else {
      error = peer.CreateClient(ClientData.SteamId!.Value, lobby.Owner.Id);
    }

    if (error != Error.Ok) {
      GD.PushError("<steam> Failed to create peer: " + error);
      return;
    }

    Initialize(peer);

    // NOTE: for client peers this will be instead called by OnConnectedToServer callback
    if (IsHost) {
      SERVER_CreatedServer?.Invoke();
    }
  }
  #endregion

  #region Peer Disconnected
  private static void OnPeerDisconnected(long disconnectedPeerId) {
    // If the host disconnects, we need to disconnect the client as well.
    if (disconnectedPeerId == 1) {
      GD.PushWarning("<multiplayer> Server disconnected from client.");
      Disconnect(MultiplayerDisconnectReason.ServerDisconnected);
      return;
    }

    // If the client disconnects, we update the player list.
    if (IsHost) {
      GD.PushWarning($"<multiplayer> Client disconnected: {disconnectedPeerId}");
      // FIXME: properly implement player disconnection
      SERVER_PlayerDisconnected?.Invoke(disconnectedPeerId);
    }
  }

  private static void OnDisconnectedFromServer() {
    if (IsHost) { throw new InvalidOperationException($"[{nameof(OnDisconnectedFromServer)}] Only clients should call this method."); }

    GD.PushWarning("<multiplayer> Disconnected from server.");
    Disconnect(MultiplayerDisconnectReason.ServerDisconnected);
  }
  #endregion
}