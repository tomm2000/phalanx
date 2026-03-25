using System;
using System.Threading.Tasks;
using Godot;
using Chickensoft.AutoInject;
using Chickensoft.Introspection;
using System.Collections.Generic;
using Tlib;
using System.Linq;



[Meta(typeof(IAutoConnect), typeof(IAutoNode))]
public partial class SingleplayerLobbyMenu : Control {
  public override void _Notification(int what) => this.Notify(what);
  public static readonly string ScenePath = "uid://dukh8gnwwqll6";
  
  [Dependency] ClientInterface ClientInterface => this.DependOn<ClientInterface>();
  [Dependency] LobbyManager LobbyManager => this.DependOn<LobbyManager>();
  [Dependency] ClientManager ClientManager => this.DependOn<ClientManager>();
  [Dependency] public ClientToServerBus ClientToServerBus => this.DependOn<ClientToServerBus>();
  [Dependency] public PlayerClientController PlayerClientController => this.DependOn<PlayerClientController>();

  #region Properties
  private bool FirstFramePassed { get; set; } = false;
  #endregion


  public void OnResolved() {
    // TODO: Implement the singleplayer lobby menu
    // LobbyManager.PlayerListUpdated += OnPlayerListUpdated;
    // OnPlayerListUpdated();

    // ClientToServerBus.RequestMapChange("phalanx:map.dev1");

    // // NOTE: This is a temporary solution to start the game immediately in singleplayer mode. for testing purposes.
    // OnStartGameButtonPressed();
  }

  public override void _Process(double delta) {
    if (!FirstFramePassed) {
      ClientToServerBus.RequestMapChange("phalanx:map.dev1");
      OnStartGameButtonPressed();
      FirstFramePassed = true;
    }
    
  }

  private void OnStartGameButtonPressed() {
    // ClientToServerBus.LobbyStartGame();
    ClientToServerBus.RequestStartGame();
  }

  private void OnQuitToMainMenuButtonPressed() {
    PlayerClientController.SwitchScene(MainMenu.ScenePath);
  }
}
