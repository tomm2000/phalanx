using System;
using System.Threading.Tasks;
using Godot;
using Chickensoft.AutoInject;
using Chickensoft.Introspection;
using Tlib.Nodes;
using System.Collections.Generic;
using Tlib;
using System.Linq;
using GodotSteam;

namespace Client.UI.Menus;

[Meta(typeof(IAutoConnect), typeof(IAutoNode))]
public partial class MultiplayerMenu : Control {
  public override void _Notification(int what) => this.Notify(what);
  public static readonly string ScenePath = "uid://c1smqhppasnum";

  [Node] private Button CreateSteamLobbyButton { get; set; } = default!;
  [Node] private LineEdit CreateIpLobbyInput { get; set; } = default!;
  [Node] private LineEdit JoinIpLobbyInput { get; set; } = default!;

  public override void _Ready() {
    if (!Steam.IsSteamRunning()) {
      CreateSteamLobbyButton.Disabled = true;
      CreateSteamLobbyButton.MouseDefaultCursorShape = CursorShape.Forbidden;
      CreateSteamLobbyButton.TooltipText = "Steam is not running";
    }
  }

  public override void _ExitTree() {
    MultiplayerManager.SERVER_ServerReady -= OnServerReady;
    MultiplayerManager.CLIENT_OnConnectionResult -= OnClientConnectionResult;
  }

  private void OnCreateSteamLobbyButtonPressed() {
    MultiplayerManager.SERVER_ServerReady += OnServerReady;
    MultiplayerManager.HostSteamLobby();
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

    MultiplayerManager.SERVER_ServerReady += OnServerReady;
    MultiplayerManager.HostEnet(portInt);
  }

  private void OnServerReady() {
    Main.Instance.SwitchScene(MultiplayerLobbyMenu.ScenePath);
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

    MultiplayerManager.CLIENT_OnConnectionResult += OnClientConnectionResult;
    MultiplayerManager.ConnectEnet(ip, port);
  }

  private void OnClientConnectionResult(ConnectionResult result) {
    if (result.Result == ConnectionResultType.Success) {
      Main.Instance.SwitchScene(MultiplayerLobbyMenu.ScenePath);
    } else {
      GD.PrintErr($"Failed to connect to server: {result.Message}");
    }
  }


  private void OnQuitToMainMenuButtonPressed() {
    Main.Instance.SwitchScene(MainMenu.ScenePath);
  }
}
