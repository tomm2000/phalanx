using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentResults;
using Godot;
using MessagePack;
using Steamworks;

[MessagePackObject]
public struct RegistrationResult {
  [Key(0)] public bool IsSuccess { get; set; }
  [Key(1)] public string Message { get; set; }

  [IgnoreMember] public readonly bool IsFailure => !IsSuccess;

  public static RegistrationResult Ok(string message = "") {
    return new RegistrationResult { IsSuccess = true, Message = message };
  }

  public static RegistrationResult Error(string message) {
    return new RegistrationResult { IsSuccess = false, Message = message };
  }

  public override string ToString() {
    return IsSuccess ? $"Success: {Message}" : $"Error: {Message}";
  }
}

public partial class PlayerManager : Node {
  private static PlayerManager Instance = default!;

  public override void _Ready() {
    Instance = this;
  }

  private Dictionary<string, Player> registeredPlayers = [];

  #region public interface
  public static event Action? PlayerListUpdated;

  // ================== Player Connection Events
  public static event Action<Player>? PlayerFirstConnected;
  public static event Action<Player>? PlayerReconnected;
  public static event Action<Player>? PlayerConnected;
  public static event Action<string>? PlayerFailedToConnect;

  // ================== Player Disconnection Events
  public static event Action<Player>? PlayerDisconnected;
  public static event Action<string>? PlayerExited;

  public static IEnumerable<Player> Players => Instance.registeredPlayers.Values;

  public static RegistrationResult SERVER_RegisterPlayer(
    string name,
    long peerId,
    ulong? steamId = null,
    PlayerType playerType = PlayerType.Human
  ) {
    if (!MultiplayerManager.IsHost) throw new Exception($"[{nameof(SERVER_RegisterPlayer)}] can only be called by the host");

    // check if player already exists
    Result<Player> existingPlayer;
    if (steamId != null) {
      existingPlayer = Players.FindBySteamID(steamId.Value);
    } else {
      existingPlayer = Players.FindByName(name);
    }

    if (existingPlayer.IsFailed) {
      //------------------- if the player does not exist, create a new player
      // TODO: check if the game stage is lobby

      string uuid = Guid.NewGuid().ToString();
      var time = DateTime.Now.Ticks;

      var newPlayer = new Player(
        uid: uuid,
        username: name,
        peerId: peerId,
        connectionStatus: ConnectionStatus.Connected,
        joinTime: time,
        playerType: playerType,
        steamId: steamId
      );
      Instance.Rpc(nameof(OnPlayerConnected), newPlayer.Serialize());

      return RegistrationResult.Ok($"Player \"{name}\" connected");

    } else if (existingPlayer.Value.ConnectionStatus == ConnectionStatus.Disconnected) {
      //------------------- if player already exists and is not connected, reconnect

      var newPlayer = existingPlayer.Value.With(
        username: name,
        peerId: peerId,
        connectionStatus: ConnectionStatus.Connected,
        playerType: playerType,
        steamId: steamId
      );

      Instance.Rpc(nameof(OnPlayerReconnected), newPlayer.Serialize());

      return RegistrationResult.Ok($"Player \"{name}\" reconnected");
    } else if (existingPlayer.Value.PlayerType == PlayerType.Bot) {
      //------------------- if player already exists and is a bot, reconnect

      // TODO: handle player replacing bot
      throw new NotImplementedException("Player replacing bot is not implemented yet");

    } else {
      //------------------- error, return (player already exists and is connected)
      GD.PushWarning($"<multiplayer> Player \"{name}\" already connected with id: {existingPlayer.Value.PeerId}");

      Instance.Rpc(nameof(OnPlayerFailedToConnect), name);

      return RegistrationResult.Error($"Player \"{name}\" already connected");
    }
  }

  /// <summary>
  /// Unregisters a player from the server. This is called when a player leaves the game.
  /// </summary>
  public static void PlayerQuit(long peerId) {
    var player = Players.FindByPeerID(peerId);
    if (player.IsFailed) return;

    PlayerQuit(player.Value.UID);
  }

  /// <summary>
  /// Unregisters a player from the server. This is called when a player leaves the game.
  /// </summary>
  public static void PlayerQuit(string uid) {
    if (!MultiplayerManager.IsHost) throw new Exception($"[{nameof(PlayerQuit)}] can only be called by the host");

    var playerExists = Instance.registeredPlayers.TryGetValue(uid, out var player);

    if (playerExists) {
      Instance.Rpc(nameof(OnPlayerExited), player.UID);
    }
  }

  /// <summary>
  /// Unregisters a player from the server. This is called when a client disconnects.
  /// </summary>
  public static void PeerDisconnected(long peerId) {
    if (!MultiplayerManager.IsHost) throw new Exception($"[{nameof(PeerDisconnected)}] can only be called by the host");

    var player = Players.FindByPeerID(peerId);
    if (player.IsFailed) return;

    var newPlayer = player.Value.With(
      connectionStatus: ConnectionStatus.Disconnected,
      peerId: -1
    );
    Instance.Rpc(nameof(OnPlayerDisconnected), newPlayer.Serialize());
  }

  public static void Reset() {
    Instance.registeredPlayers.Clear();
  }
  #endregion

  #region Client Callbacks
  [Rpc(
    mode: MultiplayerApi.RpcMode.Authority,
    CallLocal = true,
    TransferMode = MultiplayerPeer.TransferModeEnum.Reliable
  )]
  private void OnPlayerConnected(byte[] playerData) {
    var player = playerData.Deserialize<Player>();

    registeredPlayers.Remove(player.UID);
    registeredPlayers.Add(player.UID, player);

    PlayerListUpdated?.Invoke();
    PlayerFirstConnected?.Invoke(player);
  }

  [Rpc(
    mode: MultiplayerApi.RpcMode.Authority,
    CallLocal = true,
    TransferMode = MultiplayerPeer.TransferModeEnum.Reliable
  )]
  private void OnPlayerReconnected(byte[] playerData) {
    var player = playerData.Deserialize<Player>();

    registeredPlayers.Remove(player.UID);
    registeredPlayers.Add(player.UID, player);

    PlayerListUpdated?.Invoke();
    PlayerReconnected?.Invoke(player);
  }

  [Rpc(
    mode: MultiplayerApi.RpcMode.Authority,
    CallLocal = true,
    TransferMode = MultiplayerPeer.TransferModeEnum.Reliable
  )]
  private void OnPlayerFailedToConnect(string name) {
    PlayerFailedToConnect?.Invoke(name);
  }

  [Rpc(
    mode: MultiplayerApi.RpcMode.Authority,
    CallLocal = true,
    TransferMode = MultiplayerPeer.TransferModeEnum.Reliable
  )]
  private void OnPlayerExited(string uid) {
    registeredPlayers.Remove(uid);

    PlayerListUpdated?.Invoke();
    PlayerExited?.Invoke(uid);
  }

  [Rpc(
    mode: MultiplayerApi.RpcMode.Authority,
    CallLocal = true,
    TransferMode = MultiplayerPeer.TransferModeEnum.Reliable
  )]
  private void OnPlayerDisconnected(byte[] playerData) {
    var player = playerData.Deserialize<Player>();
    registeredPlayers.Remove(player.UID);
    registeredPlayers.Add(player.UID, player);

    PlayerListUpdated?.Invoke();
    PlayerDisconnected?.Invoke(player);
  }
  #endregion

  #region Synchronization
  public static void SERVER_SynchronizeClient(long peerId) {
    if (!MultiplayerManager.IsHost) { return; }

    var playerList = Players.ToArray();
    Instance.RpcId(peerId, nameof(CLIENT_PlayerListSync), playerList.Serialize());
  }

  [Rpc(
    mode: MultiplayerApi.RpcMode.Authority,
    CallLocal = true,
    TransferMode = MultiplayerPeer.TransferModeEnum.Reliable
  )]
  private void CLIENT_PlayerListSync(byte[] playerData) {
    var player = playerData.Deserialize<Player[]>();

    registeredPlayers.Clear();

    foreach (var p in player) {
      registeredPlayers.Add(p.UID, p);
    }

    PlayerListUpdated?.Invoke();
  }
  #endregion
}