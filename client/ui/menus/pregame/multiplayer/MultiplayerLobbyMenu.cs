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

  public override void _Ready() {
    PlayerManager.PlayerListUpdated += OnPlayerListUpdated;
    MultiplayerManager.Disconnected += ReturnToMultiplayerMenu;

    OnPlayerListUpdated();
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
  }

  private void OnExitLobbyButtonPressed() {
    MultiplayerManager.Disconnect();
    
    ReturnToMultiplayerMenu();
  }

  private void ReturnToMultiplayerMenu() {
    Main.Instance.SwitchScene(MultiplayerMenu.ScenePath);
  }
}
