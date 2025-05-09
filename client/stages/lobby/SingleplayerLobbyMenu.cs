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
public partial class SingleplayerLobbyMenu : Control {
  public override void _Notification(int what) => this.Notify(what);
  public static readonly string ScenePath = "uid://dukh8gnwwqll6";
  
  [Dependency] GameInstance GameInstance => this.DependOn<GameInstance>();
  [Dependency] ClientToServerBus ClientToServerBus => this.DependOn<ClientToServerBus>();
  [Dependency] PlayerManager PlayerManager => this.DependOn<PlayerManager>();

  public void OnResolved() {
    // TODO: Implement the singleplayer lobby menu
    // PlayerManager.PlayerListUpdated += OnPlayerListUpdated;
    // OnPlayerListUpdated();

    ClientToServerBus.LobbySelectMap("devmap");

    // NOTE: This is a temporary solution to start the game immediately in singleplayer mode. for testing purposes.
    OnStartGameButtonPressed();
  }

  private void OnStartGameButtonPressed() {
    ClientToServerBus.LobbyStartGame();
  }

  private void OnQuitToMainMenuButtonPressed() {
    Main.SwitchScene(MainMenu.ScenePath);
  }
}
