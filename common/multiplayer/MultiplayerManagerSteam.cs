using System.Threading.Tasks;
using Godot;
using Steam;
using Steamworks;
using Steamworks.Data;

public partial class MultiplayerManager : Node {
  public static Lobby? ActiveSteamLobby { get; private set; } = null;


  #region Lobby Creation
  public static async Task HostSteam() {
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

  private static void OnSteamLobbyCreated(Result result, Lobby lobby) { }
  #endregion

  #region Lobby Joining
  public static void ConnectSteam(Lobby lobby) {
    lobby.Join();
  }

  private static void OnSteamLobbyEntered(Lobby lobby) {
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

  private static void LeaveSteamLobby() {
    GD.Print("<steam> Leaving lobby...");
    
    if (ActiveSteamLobby is Lobby lobby) {
      lobby.Leave();
      ActiveSteamLobby = null;
    }
  }
}