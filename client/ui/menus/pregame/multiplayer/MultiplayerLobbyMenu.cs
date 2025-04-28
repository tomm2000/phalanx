using System;
using System.Threading.Tasks;
using Godot;
using Chickensoft.AutoInject;
using Chickensoft.Introspection;
using Tlib.Nodes;
using System.Collections.Generic;
using Tlib;
using System.Linq;

namespace Client.UI.Menus;

[Meta(typeof(IAutoConnect), typeof(IAutoNode))]
public partial class MultiplayerLobbyMenu : Control {
  public override void _Notification(int what) => this.Notify(what);
  public static readonly string ScenePath = "uid://dr1b75c32sida";

  [Node] private Label LobbyNameLabel { get; set; } = default!;
  [Node] private Control PlayerList { get; set; } = default!;
  [Node] private Button ReadyButton { get; set; } = default!;
  [Node] private Button StartButton { get; set; } = default!;

  public Dictionary<string, bool> PlayerReadyStatus { get; set; } = [];

  public override void _Ready() {
    PlayerManager.PlayerListUpdated += OnPlayerListUpdated;
    MultiplayerManager.Disconnected += ReturnToMultiplayerMenu;

    OnPlayerListUpdated();

    if (MultiplayerManager.IsHost) {
      UpdateStartButton();
    } else {
      StartButton.Visible = false;
    }
  }

  public override void _ExitTree() {
    PlayerManager.PlayerListUpdated -= OnPlayerListUpdated;
    MultiplayerManager.Disconnected -= ReturnToMultiplayerMenu;
  }

  private void OnPlayerListUpdated() {
    foreach (var child in PlayerList.GetChildrenOfType<PlayerListItem>()) {
      child.QueueFree();
    }

    foreach (var player in PlayerManager.Players.SortByJoinTime()) {
      var playerListItem = PlayerListItem.Instantiate(player);
      PlayerList.AddChild(playerListItem);
    }

    UpdateStartButton();
  }

  private void OnExitLobbyButtonPressed() => MultiplayerManager.Reset(MultiplayerResetReason.Disconnect);

  private void ReturnToMultiplayerMenu(MultiplayerResetReason reason) {
    // Main.SwitchScene(MultiplayerMenu.ScenePath);
    if (reason == MultiplayerResetReason.Disconnect || reason == MultiplayerResetReason.None) {
      Main.SwitchScene(MultiplayerMenu.ScenePath);

    } else if (reason == MultiplayerResetReason.ServerDisconnected) {
      var loadingScreen = MenuLoadingScreen.Instantiate(
      text: "Disconnected from server.",
      buttonText: "Return to multiplayer menu",
      timeout: 0,
      nextScene: MultiplayerMenu.ScenePath
      );
      Main.SwitchScene(loadingScreen);
    } else if (reason == MultiplayerResetReason.Error) {
      var loadingScreen = MenuLoadingScreen.Instantiate(
      text: "An error occurred.",
      buttonText: "Return to multiplayer menu",
      timeout: 0,
      nextScene: MultiplayerMenu.ScenePath
      );
      Main.SwitchScene(loadingScreen);
    } else {
      throw new ArgumentOutOfRangeException(nameof(reason), reason, null);
    }
  }

  #region Start/Ready
  private void OnReadyButtonPressed() {
    RpcId(1, nameof(SERVER_ClientReadyUp));
  }

  private void OnStartGameButtonPressed() {
    if (!MultiplayerManager.IsHost) { throw new Exception("Only the host can start the game."); }

    var allReady = PlayerManager.Players.All(p => PlayerReadyStatus.GetValueOrDefault(p.UID, false));
    if (!allReady) { return; }

    var map = DevMap.GenerateMap(19, 19, seed: 1234);

    Rpc(nameof(CLIENT_StartGame), map.Serialize());
  }

  [Rpc(
    mode: MultiplayerApi.RpcMode.AnyPeer,
    CallLocal = true,
    TransferMode = MultiplayerPeer.TransferModeEnum.Reliable
  )]
  private void SERVER_ClientReadyUp() {
    var peerId = MultiplayerManager.RpcSenderId();
    var playerResult = PlayerManager.Players.FindByPeerID(peerId);

    if (playerResult.IsFailed) {
      throw new Exception($"Player with peer ID {peerId} not found.");
    }

    var player = playerResult.Value;
    var newStatus = !PlayerReadyStatus.GetValueOrDefault(player.UID, false);

    Rpc(nameof(CLIENT_PlayerStatusUpdate), player.UID, newStatus);
  }

  [Rpc(
    mode: MultiplayerApi.RpcMode.Authority,
    CallLocal = true,
    TransferMode = MultiplayerPeer.TransferModeEnum.Reliable
  )]
  private void CLIENT_PlayerStatusUpdate(string playerUID, bool isReady) {
    PlayerReadyStatus[playerUID] = isReady;

    UpdateStartButton();
  }

  [Rpc(
    mode: MultiplayerApi.RpcMode.Authority,
    CallLocal = true,
    TransferMode = MultiplayerPeer.TransferModeEnum.Reliable
  )]
  private void CLIENT_StartGame(byte[] mapData) {
    var map = mapData.Deserialize<MapData>();

    var clientGameManager = GameClientManager.Instantiate(map);
    Main.SwitchScene(clientGameManager);
  }

  private void UpdateStartButton() {
    bool allReady = PlayerManager.Players.All(p => PlayerReadyStatus.GetValueOrDefault(p.UID, false));
    StartButton.Disabled = !allReady;
  }
  #endregion
}
