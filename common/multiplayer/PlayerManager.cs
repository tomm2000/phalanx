using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FluentResults;
using Godot;
using Steamworks;

public partial class PlayerManager : Node {
  #region Properties
  private static PlayerManager? _instance;
  public static PlayerManager Instance {
    get => _instance ?? throw new Exception("PlayerService instance not found");
    private set => _instance = value;
  }
  #endregion

  #region Events
  public static Action? PlayerListUpdated;

  // ================== Player Connection Events
  public static Action<Player>? PlayerFirstConnected;
  public static Action<string>? PlayerReconnected;
  public static Action<Player>? PlayerConnected;
  public static Action<string>? PlayerFailedToConnect;

  // ================== Player Disconnection Events
  public static Action<string>? PlayerDisconnected;
  public static Action<Player>? PlayerExited;
  #endregion

  #region Properties
  private static Dictionary<string, Player> players = new Dictionary<string, Player>();
  #endregion

  #region Accessors
  public static IEnumerable<Player> Players => players.Values;
  public static Player CurrentPlayer => Players.FindByPeerID(MultiplayerManager.PeerId).Value;
  public static Player RpcSenderPlayer() => Players.FindByPeerID(MultiplayerManager.RpcSenderId()).Value;
  public static Player GetPlayer(string uid) => Players.FindByUID(uid).Value;
  #endregion

  #region Initialization
  public override void _Ready() {
    Instance = this;
  }

  public static void Reset() {
    players.Clear();

    PlayerListUpdated?.Invoke();
  }
  #endregion

  #region connect Logic
  public static ConnectionResult SERVER_SteamPlayerConnected(ulong steamId, long peerId, string name) {
    var existingPlayer = Players.FindBySteamID(steamId);
    return SERVER_PlayerConnected(existingPlayer, name, peerId, steamId);
  }

  public static ConnectionResult SERVER_EnetPlayerConnected(string name, long peerId) {
    var existingPlayer = Players.FindByName(name);
    return SERVER_PlayerConnected(existingPlayer, name, peerId);
  }
  

  /// <summary>
  /// Call when a player connects to the server. <br/>
  /// Handles player connection logic.
  /// </summary>
  private static ConnectionResult SERVER_PlayerConnected(Result<Player> player, string name, long peerId, ulong? steamId = null) {
    if (!MultiplayerManager.IsHost) { return ConnectionResult.Failure("Only the host can connect players"); }

    if (player.IsFailed) {
      //------------------- if the player does not exist, create a new player

      string uuid = Guid.NewGuid().ToString();
      var time = DateTime.Now.Ticks;

      var newPlayer = new Player(uuid, name, peerId, ConnectionStatus.Connected, time, steamId);

      Instance.Rpc(nameof(CLIENT_PlayerConnected), newPlayer.Serialize());

      return ConnectionResult.Success($"Player \"{name}\" connected");

    } else if (player.Value.ConnectionStatus == ConnectionStatus.Disconnected) {
      //------------------- if player already exists but is disconnected, reconnect

      Instance.Rpc(nameof(CLIENT_PlayerReconnected), player.Value.UID, peerId);

      return ConnectionResult.Success($"Player \"{name}\" reconnected");
    } else {
      //------------------- error, return

      Instance.Rpc(nameof(CLIENT_PlayerFailedToConnect), name);

      return ConnectionResult.Failure($"Player \"{name}\" already connected");
    }
  }

  /// <summary>
  /// Called when a NEW player connects to the server. <br/>
  /// </summary>
  [Rpc(
    mode: MultiplayerApi.RpcMode.Authority,
    CallLocal = true,
    TransferMode = MultiplayerPeer.TransferModeEnum.Reliable
  )]
  private void CLIENT_PlayerConnected(byte[] playerData) {
    // Logging.MultiplayerLog($"Player \"{player.Username}\" {player.PeerId} connected", "CLIENT_PlayerConnected");

    var player = playerData.Deserialize<Player>();

    players.Add(player.UID, player);

    PlayerListUpdated?.Invoke();
    PlayerFirstConnected?.Invoke(player);
    PlayerConnected?.Invoke(player);
  }

  /// <summary>
  /// Called when a player reconnects to the server after being disconnected. <br/>
  /// </summary>
  [Rpc(
    mode: MultiplayerApi.RpcMode.Authority,
    CallLocal = true,
    TransferMode = MultiplayerPeer.TransferModeEnum.Reliable
  )]
  private void CLIENT_PlayerReconnected(string uid, long peerId) {
    // Logging.MultiplayerLog($"Player \"{player.Username}\" {player.PeerId} reconnected", "CLIENT_PlayerReconnected");

    var oldPlayer = players[uid];
    players[uid] = oldPlayer.With(peerId: peerId, connectionStatus: ConnectionStatus.Connected);
    
    PlayerListUpdated?.Invoke();
    PlayerReconnected?.Invoke(players[uid].Username);
    PlayerConnected?.Invoke(players[uid]);
  }

  [Rpc(
    mode: MultiplayerApi.RpcMode.Authority,
    CallLocal = true,
    TransferMode = MultiplayerPeer.TransferModeEnum.Reliable
  )]
  private void CLIENT_PlayerFailedToConnect(string name) {
    PlayerFailedToConnect?.Invoke(name);
  }
  #endregion

  #region disconnect Logic
  /// <summary>
  /// Call when a player disconnects from the server. <br/>
  /// Handles player disconnection logic.
  /// </summary>
  public static void SERVER_PlayerDisconnected(long peerId) {
    if (!MultiplayerManager.IsHost) { return; }
    
    var player = Players.FindByPeerID(peerId);

    if (player.IsFailed) {
      throw new Exception($"<peer {MultiplayerManager.PeerId}> [SERVER_PeerDisconnected] Player not found");
    } else {
      // Logging.MultiplayerLog($"Player \"{player.Value.Username}\" {player.Value.PeerId} disconnected", "SERVER_PeerDisconnected");

      Instance.Rpc(nameof(CLIENT_PlayerExit), player.Value.UID);
    }
  }

  /// <summary>
  /// Called when a player exits the game. <br/>
  /// </summary>
  [Rpc(
    mode: MultiplayerApi.RpcMode.Authority,
    CallLocal = true,
    TransferMode = MultiplayerPeer.TransferModeEnum.Reliable
  )]
  private void CLIENT_PlayerExit(string uid) {
    // Logging.MultiplayerLog($"Player \"{player.Username}\" {player.PeerId} exited", "CLIENT_PlayerExit");

    var player = players[uid];

    players.Remove(uid);

    PlayerListUpdated?.Invoke();
    PlayerExited?.Invoke(player);
  }

  /// <summary>
  /// Called when a player disconnects from the server. <br/>
  /// </summary>
  [Rpc(
    mode: MultiplayerApi.RpcMode.Authority,
    CallLocal = true,
    TransferMode = MultiplayerPeer.TransferModeEnum.Reliable
  )]
  private void CLIENT_PlayerDisconnected(string uid) {
    // FIXME: Implement CLIENT_PlayerDisconnected
    throw new NotImplementedException();
  }
  #endregion

  #region Synchronization
  public static void SERVER_SynchronizeClient(long peerId) {
    if (!MultiplayerManager.IsHost) { return; }

    foreach (var player in Players) {
      Instance.RpcId(peerId, nameof(CLIENT_PlayerListSync), player.Serialize());
    }
  }

  [Rpc(
    mode: MultiplayerApi.RpcMode.Authority,
    CallLocal = true,
    TransferMode = MultiplayerPeer.TransferModeEnum.Reliable
  )]
  private void CLIENT_PlayerListSync(byte[] playerData) {
    var player = playerData.Deserialize<Player>();

    players[player.UID] = player;

    PlayerListUpdated?.Invoke();
  }
  #endregion
}