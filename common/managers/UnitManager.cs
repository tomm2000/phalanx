using System;
using System.Collections.Generic;
using System.Linq;
using Chickensoft.AutoInject;
using Chickensoft.Introspection;

using Godot;
using Steamworks;
using Tlib.Hex;
using Tlib.NodeExt;
using Tlib.Serialization;

[Meta(typeof(IAutoConnect), typeof(IAutoNode))]
public partial class UnitManager : Node {
  public override void _Notification(int what) => this.Notify(what);
  
  #region Nodes
  [Dependency] Main Main => this.DependOn<Main>();
  [Dependency] ClientManager ClientManager => this.DependOn<ClientManager>();
  [Dependency] NetStateManager NetStateManager => this.DependOn<NetStateManager>();
  [Dependency] NetMessageManager NetEventManager => this.DependOn<NetMessageManager>();
  [Dependency] ScenarioManager ScenarioManager => this.DependOn<ScenarioManager>();
  #endregion

  #region Properties
  private Dictionary<string, UnitInstance> _units = [];
  public IReadOnlyDictionary<string, UnitInstance> Units {
    get {
      if (MultiplayerManager.IsHost) {
        return _units;
      }
      GD.PrintErr("Attempted to access units on client. This is not allowed.");
      return new Dictionary<string, UnitInstance>();
    }
  }
  #endregion

  #region Events
  #endregion

  #region Lifecycle
  public void OnResolved() {
    // // ==================================
    // var requestUnitDeploy = new ClientToServerMessage<UnitInstanceID>("UnitDeploy", "test-client-id");
    // requestUnitDeploy.LinkManager(NetEventManager);

    // // on the client
    // requestUnitDeploy.CLIENT_Send("test-unit-id");

    // // on the server
    // requestUnitDeploy.SERVER_OnMessage += (unitID, senderClientID, senderPeerID) => {
    //   // deploy the unit on the server
    // };

    // // ==================================

    // var unitDeployed = new ServerToClientMessage<UnitInstanceID>("UnitDeployed", "test-client-id");
    // unitDeployed.LinkManager(NetEventManager);

    // // on the server
    // unitDeployed.SERVER_Send("test-unit-id");

    // // on the client
    // unitDeployed.CLIENT_OnMessage += (unitID) => {
    //   // show the unit on the client
    // };
    // // ==================================
  }
  #endregion

  #region Helpers
  private void ValidateIsHost() {
    if (!MultiplayerManager.IsHost) {
      throw new InvalidOperationException("This operation can only be performed on the host.");
    }
  }
  private void TryGetClientInterface(ClientID clientID, out ClientInterface clientInterface) {
    if (Main.Instance.GetClientInterface(clientID) is ClientInterface ci) {
      clientInterface = ci;
    } else {
      throw new ArgumentException($"Failed to get client interface for client {clientID}", nameof(clientID));
    }
  }

  private void TryGetScenario(out Scenario scenario) {
    if (ScenarioManager.SelectedScenario == null) {
      throw new InvalidOperationException("No scenario selected.");
    }
    scenario = ScenarioManager.SelectedScenario.Value!.Value;
  }
  
  public void TryGetUnitBlueprint(string blueprintID, out UnitBlueprint blueprint) {
    TryGetScenario(out Scenario scenario);

    if (!scenario.Units.ContainsKey(blueprintID)) {
      throw new ArgumentException("Unit blueprint not found.", nameof(blueprintID));
    }
    blueprint = scenario.Units[blueprintID];
  }
  #endregion

  #region Visibility
  public bool IsUnitVisibleToPlayer(UnitInstanceID unitID, ClientID playerID) {
    return true;
  }

  public IEnumerable<Client> GetClientsWithVisibilityOfUnit(UnitInstanceID unitID) {
    return ClientManager.Clients.Where(client => IsUnitVisibleToPlayer(unitID, client.UID));
  }
  #endregion

  #region Sync
  public void SERVER_SyncClient(ClientID clientID) {
    ValidateIsHost();

    var units = Units.Values.ToList();

    TryGetClientInterface(clientID, out ClientInterface clientInterface);

    clientInterface.ServerToClientBus.SERVER_SyncClientUnits(units);
  }
  #endregion

  #region Deployment
  public void SERVER_DeployUnit(ClientID clientID, DatabaseEntryString blueprintID, HexCoords position) {
    ValidateIsHost();

    TryGetScenario(out Scenario scenario);
    TryGetUnitBlueprint(blueprintID, out UnitBlueprint blueprint);

    var unitID = Guid.NewGuid().ToString();
    var unitInstance = new UnitInstance(
      unitID,
      blueprintID,
      clientID,
      position
    );

    _units[unitID] = unitInstance;

    var clientsToNotify = GetClientsWithVisibilityOfUnit(unitID);
    foreach (var client in clientsToNotify) {
      TryGetClientInterface(client.UID, out ClientInterface clientInterface);
      clientInterface.ServerToClientBus.SERVER_UnitDeployed(unitInstance);
    }
  }
  #endregion
}