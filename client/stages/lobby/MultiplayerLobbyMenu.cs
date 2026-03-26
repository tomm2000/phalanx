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
  [Dependency] public PlayerClientController PlayerClientController => this.DependOn<PlayerClientController>();

  public void OnResolved() {
    ClientManager.ClientListUpdated += OnClientListUpdated;
    LobbyManager.PlayerReadyStatuses.OnValueChanged += UpdateStartButton;
    MultiplayerManager.Disconnected += ReturnToMultiplayerMenu;

    OnClientListUpdated();

    if (ClientInterface.IsMaster) {
      UpdateStartButton();
    } else {
      StartButton.Visible = false;
    }
  }


  public override void _ExitTree() {
    ClientManager.ClientListUpdated -= OnClientListUpdated;
    LobbyManager.PlayerReadyStatuses.OnValueChanged -= UpdateStartButton;
    MultiplayerManager.Disconnected -= ReturnToMultiplayerMenu;
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

  private void OnExitLobbyButtonPressed() => MultiplayerManager.Disconnect(MultiplayerDisconnectReason.UserRequested);

  private void ReturnToMultiplayerMenu(MultiplayerDisconnectReason reason) {
    if (reason == MultiplayerDisconnectReason.UserRequested) {
      PlayerClientController.SwitchScene(MultiplayerMenu.ScenePath);

    } else if (reason == MultiplayerDisconnectReason.ServerDisconnected) {
      var loadingScreen = MenuLoadingScreen.Instantiate(
      text: "Disconnected from server.",
      buttonText: "Return to multiplayer menu",
      timeout: 0,
      nextScene: MultiplayerMenu.ScenePath
      );
      PlayerClientController.SwitchScene(loadingScreen);
    } else if (reason == MultiplayerDisconnectReason.Error) {
      var loadingScreen = MenuLoadingScreen.Instantiate(
      text: "An error occurred.",
      buttonText: "Return to multiplayer menu",
      timeout: 0,
      nextScene: MultiplayerMenu.ScenePath
      );
      PlayerClientController.SwitchScene(loadingScreen);
    } else {
      throw new ArgumentOutOfRangeException(nameof(reason), reason, null);
    }
  }

  #region Start/Ready
  private void OnReadyButtonPressed() {
    bool currentReady = LobbyManager.PlayerReadyStatuses.GetValueOrDefault(ClientInterface.ClientID, false);

    ClientInterface.SendMessageToServer(NetMessageID.RequestLobbyReadyStatusChange, !currentReady);
  }

  private void OnStartGameButtonPressed() {
    if (!ClientInterface.IsMaster) { throw new Exception("Only the host can start the game."); }

    var allReady = ClientManager
      .Clients
      .All(c => LobbyManager.PlayerReadyStatuses.GetValueOrDefault(c.UID, false));
    if (!allReady) { return; }

    ClientInterface.SendMessageToServer(NetMessageID.RequestStartGame, true);
  }

  private void UpdateStartButton() {
    bool allReady = ClientManager
      .Clients
      .All(c => LobbyManager.PlayerReadyStatuses.GetValueOrDefault(c.UID, false));
    StartButton.Disabled = !allReady;
  }
  #endregion
}
