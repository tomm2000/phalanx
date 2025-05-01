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
  
  [Dependency] public GameInstance GameInstance => this.DependOn<GameInstance>();
  [Dependency] public ClientToServerInterface ClientToServerInterface => this.DependOn<ClientToServerInterface>();

  public void OnResolved() {
    // TODO: Implement the singleplayer lobby menu
    // PlayerManager.PlayerListUpdated += OnPlayerListUpdated;
    // OnPlayerListUpdated();

    ClientToServerInterface.SelectGameMap("devmap");
  }

  private void OnStartGameButtonPressed() {
    GameInstance.SERVER_ChangeGameStage(GameStage.Battle);
  }

  private void OnQuitToMainMenuButtonPressed() {
    Main.SwitchScene(MainMenu.ScenePath);
  }
}
