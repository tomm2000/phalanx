using System;
using System.Threading.Tasks;
using Godot;
using Chickensoft.AutoInject;
using Chickensoft.Introspection;
using Tlib.Nodes;
using System.Collections.Generic;
using Tlib;
using System.Linq;
using Steamworks;
using Steamworks.Data;

namespace Client.UI;

[Meta(typeof(IAutoConnect), typeof(IAutoNode))]
public partial class MultiplayerMenu : Control {
  public override void _Notification(int what) => this.Notify(what);
  public static readonly string ScenePath = "uid://c1smqhppasnum";

  [Node] private Button CreateSteamLobbyButton { get; set; } = default!;
  [Node] private LineEdit CreateIpLobbyInput { get; set; } = default!;
  [Node] private LineEdit JoinIpLobbyInput { get; set; } = default!;
  [Node] private Control LobbyList { get; set; } = default!;

  public override void _Ready() {
    MultiplayerManager.RegistrationResult += OnRegistrationResult;

    if (!SteamClient.IsValid) {
      CreateSteamLobbyButton.Disabled = true;
      CreateSteamLobbyButton.MouseDefaultCursorShape = CursorShape.Forbidden;
      CreateSteamLobbyButton.TooltipText = "Steam is not running";
    } else {
      UpdateLobbyList();
    }
  }

  public override void _ExitTree() {
    MultiplayerManager.RegistrationResult -= OnRegistrationResult;
  }

  private void OnRegistrationResult(RegistrationResult result) {
    if (result.IsSuccess) {
      Main.SwitchScene(MultiplayerLobbyMenu.ScenePath);
    } else {
      GD.PrintErr($"[{nameof(MultiplayerMenu)}] Registration failed: {result}");
    }
  }

  private void OnCreateSteamLobbyButtonPressed() {
    MultiplayerManager.HostSteam();
  }

  private void OnCreateIpLobbyButtonPressed() {
    var port = CreateIpLobbyInput.Text;

    if (!int.TryParse(port, out var portInt)) {
      GD.PrintErr("Invalid port number");
      return;
    }

    if (portInt < 1024 || portInt > 65535) {
      GD.PrintErr("Port number must be between 1024 and 65535");
      return;
    }

    MultiplayerManager.HostEnet(portInt);
  }

  private void OnJoinIpLobbyButtonPressed() {
    var address = JoinIpLobbyInput.Text;

    if (string.IsNullOrEmpty(address)) {
      GD.PrintErr("Invalid address");
      return;
    }

    var addressParts = address.Split(':');
    var ip = addressParts[0];
    var port = addressParts.Length > 1 ? int.Parse(addressParts[1]) : 7777;

    MultiplayerManager.JoinEnet(ip, port);
  }


  private void OnQuitToMainMenuButtonPressed() {
    Main.SwitchScene(MainMenu.ScenePath);
  }

  private async void UpdateLobbyList() {
    if (!SteamClient.IsValid) { return; }

    foreach (var child in LobbyList.GetChildrenOfType<LobbyListItem>()) {
      child.QueueFree();
    }

    LobbyQuery query = new();
    query.WithKeyValue("game", "phalanx");
    query.WithMaxResults(10);

    var lobbies = await query.RequestAsync();

    if(lobbies == null) {
      GD.Print("<steam> No lobbies found");
      return;
    }

    var newLobbyList = new List<LobbyListItem>();

    foreach (var lobby in lobbies) {
      var lobbyListItem = LobbyListItem.Instantiate(lobby);
      newLobbyList.Add(lobbyListItem);
    }

    LobbyList.QueueFreeChildren();
    foreach (var lobbyListItem in newLobbyList) {
      LobbyList.AddChild(lobbyListItem);
    }
  }
}
