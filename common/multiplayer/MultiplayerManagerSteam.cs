using Godot;
using GodotSteam;

public partial class MultiplayerManager : Node {
  public static void HostSteamLobby() {
    Steam.JoinRequested += OnLobbyJoinRequested;
    Steam.LobbyCreated += OnLobbyCreated;
    Steam.LobbyChatUpdate += OnLobbyChatUpdate;
    Steam.LobbyDataUpdate += OnLobbyDataUpdate;
    Steam.LobbyInvite += OnLobbyInvite;
    Steam.LobbyJoined += OnLobbyJoined;
    Steam.LobbyMatchList += OnLobbyMatchList;
    Steam.LobbyMessage += OnLobbyMessage;
    Steam.PersonaStateChange += OnPersonaChange;



    GD.Print("Creating lobby...");
    // TODO: add settings for lobby type and max players
    Steam.CreateLobby(Steam.LobbyType.Public);
  }

  private static void OnPersonaChange(ulong steamId, PersonaChange flags) { }
  private static void OnLobbyMessage(ulong lobbyId, long user, string message, long chatType) { }
  private static void OnLobbyJoined(ulong lobby, long permissions, bool locked, long response) { }
  private static void OnLobbyMatchList(Godot.Collections.Array lobbies) { }
  private static void OnLobbyInvite(ulong inviter, ulong lobby, ulong game) { }
  private static void OnLobbyDataUpdate(uint success, ulong lobbyID, ulong memberID) { }
  private static void OnLobbyChatUpdate(ulong lobbyId, long changedId, long makingChangeId, long chatState) { }
  private static void OnLobbyJoinRequested(ulong lobbyId, ulong steamId) { }

  private static void OnLobbyCreated(long connect, ulong lobbyId) {
    Steam.LobbyCreated -= OnLobbyCreated;

    if (connect == 1) {
      GD.Print("Lobby created successfully: " + lobbyId);

      Steam.SetLobbyData(lobbyId, "hostname", ClientData.Username);
      Steam.SetLobbyData(lobbyId, "game", "phalanx");
      Steam.AllowP2PPacketRelay(true);

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


  /// <summary>
  /// Called by the client to notify the server of a new connection.
  /// </summary>
  [Rpc(
    mode: MultiplayerApi.RpcMode.AnyPeer,
    CallLocal = true,
    TransferMode = MultiplayerPeer.TransferModeEnum.Reliable
  )]
  private void SERVER_SteamClientConnected(ulong steamId) {
    if (!IsServer) return;

    var peerId = Multiplayer.GetRemoteSenderId();
    var result = PlayerManager.SERVER_SteamPlayerConnected(steamId, peerId);

    SERVER_ClientConnected(result, peerId);
  }
}