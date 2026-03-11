using System;
using System.Threading.Tasks;
using Godot;
using Chickensoft.AutoInject;
using Chickensoft.Introspection;
using System.Collections.Generic;
using Tlib;
using System.Linq;
using Tlib.NodeExt;



[Meta(typeof(IAutoConnect), typeof(IAutoNode))]
public partial class MultiplayerLobbyMenu : Control {
  public override void _Notification(int what) => this.Notify(what);
  public static readonly string ScenePath = "uid://dr1b75c32sida";

  [Node] private Label LobbyNameLabel { get; set; } = default!;
  [Node] private Control PlayerList { get; set; } = default!;
  [Node] private Button ReadyButton { get; set; } = default!;
  [Node] private Button StartButton { get; set; } = default!;

  [Dependency] public ClientManager ClientManager => this.DependOn<ClientManager>();
  [Dependency] public ClientInterface ClientInterface => this.DependOn<ClientInterface>();
  [Dependency] public LobbyManager LobbyManager => this.DependOn<LobbyManager>();
  [Dependency] public ClientToServerBus ClientToServerBus => this.DependOn<ClientToServerBus>();
  [Dependency] public ScenarioManager ScenarioManager => this.DependOn<ScenarioManager>();
  [Dependency] public GameInstance GameInstance => this.DependOn<GameInstance>();

  public void OnResolved() {
    ClientManager.ClientListUpdated += OnClientListUpdated;
    MultiplayerManager.CLIENT_Disconnected += ReturnToMultiplayerMenu;
    LobbyManager.PlayerReadyStatuses.OnValueChanged += UpdateStartButton;

    OnClientListUpdated();

    if (ClientInterface.IsMaster) {
      UpdateStartButton();
    } else {
      StartButton.Visible = false;
    }
  }


  public override void _ExitTree() {
    ClientManager.ClientListUpdated -= OnClientListUpdated;
    MultiplayerManager.CLIENT_Disconnected -= ReturnToMultiplayerMenu;
  }

  private void OnClientListUpdated() {
    foreach (var child in PlayerList.GetChildren<PlayerListItem>()) {
      child.QueueFree();
    }

    foreach (var client in ClientManager.Clients.SortByJoinTime()) {
      var playerListItem = PlayerListItem.Instantiate(client);
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
    bool currentReady = LobbyManager.PlayerReadyStatuses.GetValueOrDefault(ClientInterface.Client.UID, false);
    ClientToServerBus.RequestReadyStatusChange(!currentReady);
  }

  private void OnStartGameButtonPressed() {
    if (!ClientInterface.IsMaster) { throw new Exception("Only the host can start the game."); }

    var allReady = ClientManager
      .Clients
      .All(c => LobbyManager.PlayerReadyStatuses.GetValueOrDefault(c.UID, false));
    if (!allReady) { return; }

    ClientToServerBus.RequestStartGame();
  }

  private void UpdateStartButton() {
    bool allReady = ClientManager
      .Clients
      .All(c => LobbyManager.PlayerReadyStatuses.GetValueOrDefault(c.UID, false));
    StartButton.Disabled = !allReady;
  }
  #endregion
}
