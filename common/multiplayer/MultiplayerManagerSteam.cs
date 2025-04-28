using Godot;
using GodotSteam;

public partial class MultiplayerManager : Node {
  public static ulong ActiveSteamLobbyId { get; private set; } = 0;

  #region Lobby Creation
  public static void HostSteamLobby() {
    Steam.LobbyCreated += OnLobbyCreated;

    GD.Print("Creating lobby...");
    // TODO: add settings for lobby type and max players
    Steam.CreateLobby(Steam.LobbyType.Public);
  }

  private static void OnLobbyCreated(long connect, ulong lobbyId) {
    Steam.LobbyCreated -= OnLobbyCreated;

    if (connect == 1) {
      GD.Print("Lobby created successfully: " + lobbyId);

      Steam.SetLobbyData(lobbyId, "hostname", ClientData.Username);
      Steam.SetLobbyData(lobbyId, "game", "phalanx");
      Steam.AllowP2PPacketRelay(true);

      ActiveSteamLobbyId = lobbyId;

      var peer = new SteamMultiplayerPeer();

      var error = peer.CreateHost(0);

      if (error != Error.Ok) {
        GD.PushError($"<steam> Failed to create host: {error}");
        return;
      }

      Initialize(peer);
    } else {
      GD.PushError($"<steam> Failed to create lobby: {connect}");
    }
  }
  #endregion

  #region Lobby Joining
  public static void JoinSteamLobby(ulong lobbyId) {
    if (lobbyId == 0) return;

    Steam.LobbyJoined += OnLobbyJoined;
    Steam.JoinLobby(lobbyId);
  }

  private static void OnLobbyJoined(ulong lobby, long permissions, bool locked, long response) {
    Steam.LobbyJoined -= OnLobbyJoined;

    if (response == 1) {
      GD.Print("Lobby joined successfully: " + lobby);

      ActiveSteamLobbyId = lobby;

      var peer = new SteamMultiplayerPeer();
      var hostId = Steam.GetLobbyOwner(lobby);

      var error = peer.CreateClient(hostId, 0, []);

      if (error != Error.Ok) {
        GD.PushError($"<steam> Failed to create client: {error}");
        return;
      }

      Initialize(peer);
    } else {
      GD.PushError($"<steam> Failed to join lobby: {response}");
    }
  }
  #endregion

  /// <summary>
  /// Called by the client to notify the server of a new connection.
  /// </summary>
  [Rpc(
    mode: MultiplayerApi.RpcMode.AnyPeer,
    CallLocal = true,
    TransferMode = MultiplayerPeer.TransferModeEnum.Reliable
  )]
  private void SERVER_SteamClientConnected(ulong steamId) {
    if (!IsHost) return;

    var peerId = Multiplayer.GetRemoteSenderId();
    var result = PlayerManager.SERVER_SteamPlayerConnected(steamId, peerId);

    SERVER_ClientConnected(result, peerId);
  }

  private static void LeaveSteamLobby() {
    if (ActiveSteamLobbyId != 0) {
      Steam.LeaveLobby(ActiveSteamLobbyId);
      ActiveSteamLobbyId = 0;
    }
  }
}