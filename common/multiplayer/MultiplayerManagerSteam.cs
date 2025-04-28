using System;
using System.Threading.Tasks;
using Godot;
using Steam;
using Steamworks;
using Steamworks.Data;

public partial class MultiplayerManager : Node {
  public static Lobby? ActiveSteamLobby { get; private set; } = null;


  #region Lobby Creation
  public static async Task HostSteamLobby() {
    // TODO: add settings for lobby type and max players, ...
    var newLobby = await SteamMatchmaking.CreateLobbyAsync(10);

    if (newLobby is not Lobby lobby) {
      GD.PushError("<steam> Failed to create lobby");
      return;
    }

    lobby.SetData("hostname", ClientData.Username);
    lobby.SetData("game", "phalanx");
    lobby.SetPublic();
    lobby.SetJoinable(true);

    GD.Print("<steam> Created lobby: " + newLobby);
  }

  private static void OnLobbyCreated(Result result, Lobby lobby) { }
  #endregion

  #region Lobby Joining
  public static void JoinSteamLobby(Lobby lobby) {
    
    // SteamMatchmaking.OnLobbyEntered += OnLobbyEntered;
    lobby.Join();
  }

  private static void OnLobbyEntered(Lobby lobby) {
    GD.Print("<steam> Entered lobby: " + lobby);
    ActiveSteamLobby = lobby;

    var peer = new SteamMultiplayerPeer();

    GD.Print(ClientData.SteamId);

    Error error;

    if (lobby.Owner.Id == ClientData.SteamId) {
      GD.Print("<steam> Creating host...");
      error = peer.CreateHost(ClientData.SteamId!.Value);
    } else {
      GD.Print("<steam> Creating client...");
      error = peer.CreateClient(ClientData.SteamId!.Value, lobby.Owner.Id);
    }

    if (error != Error.Ok) {
      GD.Print("<steam> Failed to create peer: " + error);
      return;
    }

    Initialize(peer);
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
  private void SERVER_SteamClientConnected(ulong steamId, string name) {
    if (!IsHost) return;

    var peerId = Multiplayer.GetRemoteSenderId();
    var result = PlayerManager.SERVER_SteamPlayerConnected(steamId, peerId, name);

    SERVER_ClientConnected(result, peerId);
  }

  private static void LeaveSteamLobby() {
    GD.Print("<steam> Leaving lobby...");
    if (ActiveSteamLobby is Lobby lobby) {
      lobby.Leave();
      ActiveSteamLobby = null;
    }
  }
}