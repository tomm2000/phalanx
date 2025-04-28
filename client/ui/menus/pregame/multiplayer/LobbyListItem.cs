using System;
using System.Collections.Generic;
using Chickensoft.AutoInject;
using Chickensoft.Introspection;
using Godot;
using Steamworks.Data;


[Meta(typeof(IAutoConnect), typeof(IAutoNode))]
public partial class LobbyListItem : Control {
  public override void _Notification(int what) => this.Notify(what);
  public static readonly string ScenePath = "uid://dcigsxlge8ir0";

  public static LobbyListItem Instantiate(Lobby lobby) {
    var scene = ResourceLoader.Load<PackedScene>(ScenePath);
    var instance = scene.Instantiate<LobbyListItem>();
    instance.lobby = lobby;
    return instance;
  }

  #region Nodes
  [Node] private Label LobbyNameLabel { get; set; } = default!;
  [Node] private Label PlayerCountLabel { get; set; } = default!;
  [Node] private Button JoinButton { get; set; } = default!;
  #endregion

  private Lobby lobby;

  public override void _Ready() {
    UpdateLobbyInfo();
  }

  public void UpdateLobbyInfo() {
    var name = lobby.GetData("hostname");
    if (string.IsNullOrEmpty(name)) {
      name = $"Unknown";
    }
    var playerLimit = lobby.MaxMembers;
    var playerCount = lobby.MemberCount;

    LobbyNameLabel.Text = name;
    PlayerCountLabel.Text = $"{playerCount}/{playerLimit}";
  }

  private void OnJoinButtonPressed() {
    MultiplayerManager.JoinSteamLobby(lobby);
  }
}
