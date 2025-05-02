using System;
using System.Collections.Generic;
using System.Linq;
using Chickensoft.AutoInject;
using Chickensoft.Introspection;
using Client;
using Godot;
using Steamworks;
using Tlib.Nodes;

[Meta(typeof(IAutoConnect), typeof(IAutoNode))]
public partial class PlayerManager : Node {
  public override void _Notification(int what) => this.Notify(what);

  #region Nodes
  [Dependency] GameInstance GameInstance => this.DependOn<GameInstance>();
  #endregion

  #region Lifecycle
  public void OnResolved() {
    MultiplayerManager.SERVER_PlayerDisconnected += OnPlayerDisconnected;

    // FIXME: temporary solution to register the player
    RegisterClient();
  }


  public override void _ExitTree() {
    MultiplayerManager.SERVER_PlayerDisconnected -= OnPlayerDisconnected;
  }
  #endregion

  #region Server
  private IEnumerable<ClientInterface> SERVER_ClientInterfaces => GameInstance.GetChildrenOfType<ClientInterface>();

  public ClientInterface? SERVER_GetClientInterface(string playerUID) {
    foreach (var clientInterface in SERVER_ClientInterfaces) {
      if (clientInterface.PlayerId == playerUID) {
        return clientInterface;
      }
    }
    return null;
  }

  public void SERVER_AttachClient(Player player) {
    if (!MultiplayerManager.IsHost) { return; }
    if (player.PeerId == 1) { return; }

    var clientInterface = ClientInterface.Instantiate(player);
    GameInstance.AddChild(clientInterface);
  }

  public void SERVER_DetachClient(string playerUID) {
    if (!MultiplayerManager.IsHost) { return; }
    var clientInterface = SERVER_GetClientInterface(playerUID);
    clientInterface?.QueueFree();
  }
  #endregion

  #region Player registration
  /// <summary>
  /// Called when a peer (either client or host) is connected to the server.
  /// </summary>
  private void RegisterClient() {
    if (SteamClient.IsValid) {
      var steamId = SteamClient.SteamId;
      var name = ClientData.Username;
      RpcId(1, nameof(SERVER_RegisterSteamPlayer), (ulong)steamId, name);
    } else {
      var name = ClientData.Username;
      RpcId(1, nameof(SERVER_RegisterEnetPlayer), name);
    }
  }

  [Rpc(
    mode: MultiplayerApi.RpcMode.AnyPeer,
    CallLocal = true,
    TransferMode = MultiplayerPeer.TransferModeEnum.Reliable
  )]
  private void SERVER_RegisterSteamPlayer(ulong steamId, string name) {
    if (!MultiplayerManager.IsHost) throw new InvalidOperationException($"[{nameof(SERVER_RegisterSteamPlayer)}] Only the host can call this method.");

    var peerId = Multiplayer.GetRemoteSenderId();
    var existingPlayer = Players.FindBySteamID(steamId);
    Player player;

    if (existingPlayer.IsFailed) {
      player = new Player(
        uid: Guid.NewGuid().ToString(),
        name: name,
        peerId: peerId,
        steamId: steamId,
        joinTime: DateTime.UtcNow.Ticks,
        playerType: PlayerType.Human,
        connectionStatus: ConnectionStatus.Connected
      );
    } else {
      player = existingPlayer.Value.With(
        peerId: peerId,
        name: name,
        playerType: PlayerType.Human
      );
    }

    SERVER_RegisterPlayer(player, existingPlayer.IsFailed);
  }

  [Rpc(
    mode: MultiplayerApi.RpcMode.AnyPeer,
    CallLocal = true,
    TransferMode = MultiplayerPeer.TransferModeEnum.Reliable
  )]
  private void SERVER_RegisterEnetPlayer(string name) {
    if (!MultiplayerManager.IsHost) throw new InvalidOperationException($"[{nameof(SERVER_RegisterEnetPlayer)}] Only the host can call this method.");

    var peerId = Multiplayer.GetRemoteSenderId();
    var existingPlayer = Players.FindByName(name);
    Player player;

    if (existingPlayer.IsFailed) {
      player = new Player(
        uid: Guid.NewGuid().ToString(),
        name: name,
        peerId: peerId,
        steamId: null,
        joinTime: DateTime.UtcNow.Ticks,
        playerType: PlayerType.Human,
        connectionStatus: ConnectionStatus.Connected
      );
    } else {
      player = existingPlayer.Value.With(
        peerId: peerId,
        name: name,
        steamId: null,
        playerType: PlayerType.Human
      );
    }

    SERVER_RegisterPlayer(player, existingPlayer.IsSuccess);
  }

  private void SERVER_RegisterPlayer(Player player, bool existingPlayer) {
    if (!existingPlayer) {
      // --------- if the player is not found, create a new one
      SERVER_AttachClient(player);

      Rpc(nameof(CLIENT_PlayerConnected), player.Serialize());

      RpcId(
        player.PeerId,
        nameof(CLIENT_RegistrationResult),
        player.Serialize(),
        true,
        "",
        Players.ToList().Serialize()
      );

    } else if (player.ConnectionStatus == ConnectionStatus.Disconnected) {
      // --------- if the player exists but is disconnected, reconnect them
      player = player.With(
        connectionStatus: ConnectionStatus.Connected,
        peerId: Multiplayer.GetRemoteSenderId()
      );

      Rpc(nameof(CLIENT_PlayerReconnected), player.Serialize());


      RpcId(
        player.PeerId,
        nameof(CLIENT_RegistrationResult),
        player.Serialize(),
        true,
        "",
        Players.ToList().Serialize()
      );

    } else {
      // --------- if the player exists and is connected, refuse the connection
      RpcId(
        player.PeerId,
        nameof(CLIENT_RegistrationResult),
        new Player().Serialize(),
        false,
        "Player already connected",
        new List<Player>().Serialize()
      );

      Rpc(nameof(CLIENT_PlayerFailedToConnect), player.Name, "Player already connected");
    }
  }

  [Rpc(
    mode: MultiplayerApi.RpcMode.Authority,
    CallLocal = true,
    TransferMode = MultiplayerPeer.TransferModeEnum.Reliable
  )]
  private void CLIENT_RegistrationResult(
    byte[] playerData,
    bool success,
    string message,
    byte[] playersData
  ) {
    if (success) {
      var currentPlayer = playerData.Deserialize<Player?>() ?? throw new InvalidOperationException($"[{nameof(CLIENT_RegistrationResult)}] Player data is null");

      GameInstance.AttachClient(currentPlayer);

      var playerList = playersData.Deserialize<List<Player>>() ?? throw new InvalidOperationException($"[{nameof(CLIENT_RegistrationResult)}] Players data is null");

      GD.Print($"Received player list: {playerList.Count} players");

      players.Clear();
      foreach (var player in playerList) {
        players.Add(player.UID, player);
      }
      PlayerListUpdated?.Invoke();

    } else {
      GD.PushError($"Failed to register player: {message}");
    }
  }
  #endregion

  #region Player disconnection
  private void OnPlayerDisconnected(long peerId) {
    if (!MultiplayerManager.IsHost) throw new InvalidOperationException($"[{nameof(OnPlayerDisconnected)}] Only the host can call this method.");

    var player = Players.FindByPeerID(peerId);

    if (player.IsFailed) {
      GD.PushError($"Player not found: {peerId}");
      return;
    }

    var playerValue = player.Value;

    // FIXME: temporary fix for player disconnection
    OnPlayerQuit(playerValue.UID);

    // if (playerValue.ConnectionStatus == ConnectionStatus.Disconnected) {
    //   GD.PushError($"Player already disconnected: {playerValue.Name}");
    //   return;
    // }

    // playerValue = playerValue.With(
    //   connectionStatus: ConnectionStatus.Disconnected,
    //   peerId: 0
    // );

    // Rpc(nameof(CLIENT_PlayerDisconnected), playerValue.Serialize());
  }

  private void OnPlayerQuit(string playerUID) {
    if (!MultiplayerManager.IsHost) throw new InvalidOperationException($"[{nameof(OnPlayerQuit)}] Only the host can call this method.");

    var player = Players.FindByUID(playerUID);

    if (player.IsFailed) {
      GD.PushError($"Player not found: {playerUID}");
      return;
    }

    var playerValue = player.Value;

    SERVER_DetachClient(playerValue.UID);
    Rpc(nameof(CLIENT_PlayerQuit), playerValue.Serialize());

    GD.Print($"Player quit: {playerValue.Name} ({playerValue.UID})");
  }
  #endregion

  #region Player list
  private Dictionary<string, Player> players = [];
  public IEnumerable<Player> Players => players.Values;

  public event Action? PlayerListUpdated;
  public event Action<Player>? PlayerConnected;
  public event Action<Player>? PlayerReconnected;
  public event Action<string, string>? PlayerFailedToConnect;
  public event Action<Player>? PlayerDisconnected;
  public event Action<Player>? PlayerQuit;

  public Player GetPlayer(string playerUID) {
    if (players.TryGetValue(playerUID, out var player)) {
      return player;
    }
    throw new InvalidOperationException($"Player not found: {playerUID}");
  }
  

  [Rpc(mode: MultiplayerApi.RpcMode.Authority, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
  private void CLIENT_PlayerConnected(byte[] playerData) {
    var player = playerData.Deserialize<Player?>();

    if (player == null) {
      throw new InvalidOperationException($"[{nameof(CLIENT_PlayerConnected)}] Player data is null");
    }

    players.Remove(player.Value.UID);
    players.Add(player.Value.UID, player.Value);

    PlayerListUpdated?.Invoke();
    PlayerConnected?.Invoke(player.Value);
  }

  [Rpc(mode: MultiplayerApi.RpcMode.Authority, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
  private void CLIENT_PlayerReconnected(byte[] playerData) {
    var player = playerData.Deserialize<Player?>();

    if (player == null) {
      throw new InvalidOperationException($"[{nameof(CLIENT_PlayerReconnected)}] Player data is null");
    }

    players.Remove(player.Value.UID);
    players.Add(player.Value.UID, player.Value);

    PlayerListUpdated?.Invoke();
    PlayerReconnected?.Invoke(player.Value);
  }

  [Rpc(mode: MultiplayerApi.RpcMode.Authority, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
  private void CLIENT_PlayerFailedToConnect(string name, string message) {
    PlayerFailedToConnect?.Invoke(name, message);
  }

  [Rpc(mode: MultiplayerApi.RpcMode.Authority, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
  private void CLIENT_PlayerDisconnected(byte[] playerData) {
    var player = playerData.Deserialize<Player?>();

    if (player == null) {
      throw new InvalidOperationException($"[{nameof(CLIENT_PlayerDisconnected)}] Player data is null");
    }

    players.Remove(player.Value.UID);
    players.Add(player.Value.UID, player.Value);

    PlayerListUpdated?.Invoke();
    PlayerDisconnected?.Invoke(player.Value);
  }

  [Rpc(mode: MultiplayerApi.RpcMode.Authority, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
  private void CLIENT_PlayerQuit(byte[] playerData) {
    var player = playerData.Deserialize<Player?>();

    if (player == null) {
      throw new InvalidOperationException($"[{nameof(CLIENT_PlayerQuit)}] Player data is null");
    }

    players.Remove(player.Value.UID);

    PlayerListUpdated?.Invoke();
    PlayerQuit?.Invoke(player.Value);
  }

  #endregion
}
