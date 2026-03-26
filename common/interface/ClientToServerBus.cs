using System;
using System.Collections.Generic;
using System.Linq;
using Chickensoft.AutoInject;
using Chickensoft.Introspection;

using Godot;
using Steamworks;
using Tlib.NodeExt;
using Tlib.Serialization;

[Meta(typeof(IAutoConnect), typeof(IAutoNode))]
public partial class ClientToServerBus : Node {
  public override void _Notification(int what) => this.Notify(what);

  #region Nodes
  [Dependency] ClientInterface ClientInterface => this.DependOn<ClientInterface>();
  [Dependency] LobbyManager LobbyManager => this.DependOn<LobbyManager>();
  [Dependency] ClientManager ClientManager => this.DependOn<ClientManager>();
  [Dependency] ScenarioManager ScenarioManager => this.DependOn<ScenarioManager>();
  [Dependency] UnitManager UnitManager => this.DependOn<UnitManager>();
  #endregion

  #region Messages
  #endregion

  #region Lifecycle
  public void OnResolved() {

  }
  #endregion

  #region Validation
  private bool ValidateRequest(out Client outClient) {
    outClient = default!;
    if (!MultiplayerManager.IsHost) return false;

    PeerID sender = MultiplayerManager.RpcSenderId();
    ClientID playerId = ClientInterface.ClientID;
    Client client = ClientManager.GetByPeerID(sender).Value;

    if (playerId != client.UID) {
      GD.PrintErr("Client ID mismatch in ready status change request.");
      return false;
    }

    outClient = client;
    return true;
  }
  #endregion

  #region Lobby
  #endregion

  #region Map
  public void RequestMapChange(string mapId) => RpcId(1, nameof(SERVER_RequestMapChange), mapId);

  [Rpc(mode: MultiplayerApi.RpcMode.AnyPeer, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
  private void SERVER_RequestMapChange(string mapId) {
    if (!ValidateRequest(out Client _)) return;
    
    // TODO: check if the sender is the master

    // TODO: here the map would get loaded from file
    // var map = DevMap.GenerateMap(19, 19, seed: 1);

    var selectedScenario = ScenarioManager.SelectedScenario.Value;
    if (selectedScenario == null) {
      GD.PrintErr("No scenario selected, cannot change map.");
      return;
    }
    var scenario = selectedScenario.Value;

    if (!scenario.Maps.TryGetValue(mapId, out var mapData)) {
      GD.PrintErr($"Map ID '{mapId}' not found in selected scenario.");
      return;
    }

    // var map = scenario.Maps[mapId];

    // TODO: get the map from the scenario that is loaded
    // MapManager.SelectedMap.SERVER_SetValue(map);

    Logger.Debug($"Map change requested for map ID: {mapId}");

    ScenarioManager.SelectedMapID.SERVER_SetValue(mapId);
  }
  #endregion

  #region Scenario
  public void RequestScenarioChange(string scenarioId) => RpcId(1, nameof(SERVER_RequestScenarioChange), scenarioId);

  [Rpc(mode: MultiplayerApi.RpcMode.AnyPeer, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
  private void SERVER_RequestScenarioChange(string scenarioId) {
    if (!ValidateRequest(out Client _)) return;

    // TODO: check if the sender is the master

    // FIXME
    // ScenarioManager.SelectedScenarioId.SERVER_SetValue(scenarioId);
  }
  #endregion

  #region Units
  public void RequestClientUnitsSync() => RpcId(1, nameof(SERVER_RequestClientUnitsSync));

  [Rpc(mode: MultiplayerApi.RpcMode.AnyPeer, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
  private void SERVER_RequestClientUnitsSync() {
    if (!ValidateRequest(out Client client)) return;

    UnitManager.SERVER_SyncClient(client.UID);
  }
  #endregion
}