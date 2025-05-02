using System;
using System.Threading.Tasks;
using Godot;
using Chickensoft.AutoInject;
using Chickensoft.Introspection;
using Tlib.Nodes;
using System.Collections.Generic;
using Tlib;
using System.Linq;

namespace Client.UI;

[Meta(typeof(IAutoConnect), typeof(IAutoNode))]
public partial class MultiplayerLobbyMenu : Control {
  public override void _Notification(int what) => this.Notify(what);
  public static readonly string ScenePath = "uid://dr1b75c32sida";

  [Node] private Label LobbyNameLabel { get; set; } = default!;
  [Node] private Control PlayerList { get; set; } = default!;
  [Node] private Button ReadyButton { get; set; } = default!;
  [Node] private Button StartButton { get; set; } = default!;

  [Dependency] public PlayerManager PlayerManager => this.DependOn<PlayerManager>();
  [Dependency] public ClientToServerBus ClientToServerBus => this.DependOn<ClientToServerBus>();
  [Dependency] public SharedDataBase SharedDataBase => this.DependOn<SharedDataBase>();
  [Dependency] public ClientInterface ClientInterface => this.DependOn<ClientInterface>();

  public void OnResolved() {
    PlayerManager.PlayerListUpdated += OnPlayerListUpdated;
    MultiplayerManager.CLIENT_Disconnected += ReturnToMultiplayerMenu;
    SharedDataBase.LobbyPlayerReadyStatus.DictionaryChanged += UpdateStartButton;

    OnPlayerListUpdated();

    if (ClientInterface.IsMaster) {
      UpdateStartButton();
      ClientToServerBus.LobbySelectMap("devmap");
    } else {
      StartButton.Visible = false;
      SharedDataBase.SelectedMap.ValueChanged += OnMapChanged;
    }
  }

  public override void _ExitTree() {
    PlayerManager.PlayerListUpdated -= OnPlayerListUpdated;
    MultiplayerManager.CLIENT_Disconnected -= ReturnToMultiplayerMenu;
  }

  private void OnMapChanged(MapData? oldValue, MapData? newValue) {
    if (newValue == null) { return; }

    GD.Print("Map changed to: ", newValue.mapName);
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

  private void OnExitLobbyButtonPressed() => MultiplayerManager.Disconnect(MultiplayerDisconnectReason.None);

  private void ReturnToMultiplayerMenu(MultiplayerDisconnectReason reason) {
    if (reason == MultiplayerDisconnectReason.None) {
      Main.SwitchScene(MultiplayerMenu.ScenePath);

    } else if (reason == MultiplayerDisconnectReason.ServerDisconnected) {
      var loadingScreen = MenuLoadingScreen.Instantiate(
      text: "Disconnected from server.",
      buttonText: "Return to multiplayer menu",
      timeout: 0,
      nextScene: MultiplayerMenu.ScenePath
      );
      Main.SwitchScene(loadingScreen);
    } else if (reason == MultiplayerDisconnectReason.Error) {
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
    var currentReady = SharedDataBase.LobbyPlayerReadyStatus.Value.GetValueOrDefault(ClientInterface.PlayerId, false);
    ClientToServerBus.LobbySetPlayerReady(!currentReady);
  }

  private void OnStartGameButtonPressed() {
    if (!ClientInterface.IsMaster) { throw new Exception("Only the host can start the game."); }

    var allReady = PlayerManager
      .Players
      .All(p => SharedDataBase.LobbyPlayerReadyStatus.Value.GetValueOrDefault(p.UID, false));
    if (!allReady) { return; }

    ClientToServerBus.LobbyStartGame();
  }

  private void UpdateStartButton() {
    bool allReady = PlayerManager
      .Players
      .All(p => SharedDataBase.LobbyPlayerReadyStatus.Value.GetValueOrDefault(p.UID, false));
    StartButton.Disabled = !allReady;
  }
  #endregion
}
