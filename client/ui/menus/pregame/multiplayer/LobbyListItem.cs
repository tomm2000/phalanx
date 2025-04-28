using System;
using System.Collections.Generic;
using Chickensoft.AutoInject;
using Chickensoft.Introspection;
using Godot;
using GodotSteam;


[Meta(typeof(IAutoConnect), typeof(IAutoNode))]
public partial class LobbyListItem : Control {
  public override void _Notification(int what) => this.Notify(what);
  public static readonly string ScenePath = "uid://dcigsxlge8ir0";

  public static LobbyListItem Instantiate(ulong lobbyId) {
    var scene = ResourceLoader.Load<PackedScene>(ScenePath);
    var instance = scene.Instantiate<LobbyListItem>();
    instance.lobbyId = lobbyId;
    return instance;
  }

  #region Nodes
  [Node] private Label LobbyNameLabel { get; set; } = default!;
  [Node] private Label PlayerCountLabel { get; set; } = default!;
  [Node] private Button JoinButton { get; set; } = default!;
  #endregion

  private ulong lobbyId;

  public override void _Ready() {
    UpdateLobbyInfo();
  }

  public void UpdateLobbyInfo() {
    var data = Steam.GetAllLobbyData(lobbyId);

    if (data.Count == 0) {
      this.QueueFree();
      return;
    }

    var name = Steam.GetLobbyData(lobbyId, "hostname");
    if (string.IsNullOrEmpty(name)) {
      name = $"Unknown";
    }
    var playerLimit = Steam.GetLobbyMemberLimit(lobbyId);
    var playerCount = Steam.GetNumLobbyMembers(lobbyId);

    LobbyNameLabel.Text = name;
    PlayerCountLabel.Text = $"{playerCount}/{playerLimit}";
  }

  private void OnJoinButtonPressed() {
    MultiplayerManager.JoinSteamLobby(lobbyId);
  }
}
