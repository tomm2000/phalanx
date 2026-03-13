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
public partial class ServerToClientBus : Node {
  public override void _Notification(int what) => this.Notify(what);

  #region Remote Values

  #endregion

  #region Nodes
  [Dependency] ClientInterface ClientInterface => this.DependOn<ClientInterface>();
  #endregion

  #region Units
  public void SERVER_SyncClientUnits(List<UnitInstance> units) {
    if (MultiplayerManager.IsHost) {
      GD.PrintErr("Attempted to sync client units on host. This is not allowed.");
      return;
    }

    RpcId(ClientInterface.Client.PeerId, nameof(RPC_SyncClientUnits), units.Serialize());
  }

  [Rpc(mode: MultiplayerApi.RpcMode.Authority, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
  public void RPC_SyncClientUnits(byte[] serializedUnits) => OnClientUnitsSynced?.Invoke(serializedUnits.Deserialize<List<UnitInstance>>());

  public Action<List<UnitInstance>>? OnClientUnitsSynced;

  // ============================================================================================

  public void SERVER_UnitDeployed(UnitInstance unitInstance) {
    if (MultiplayerManager.IsHost) {
      GD.PrintErr("Attempted to notify host of unit deployment. This is not allowed.");
      return;
    }

    RpcId(ClientInterface.Client.PeerId, nameof(RPC_UnitDeployed), unitInstance.Serialize());
  }  

  [Rpc(mode: MultiplayerApi.RpcMode.Authority, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
  public void RPC_UnitDeployed(byte[] serializedUnitInstance) => OnUnitDeployed?.Invoke(serializedUnitInstance.Deserialize<UnitInstance>());

  public Action<UnitInstance>? OnUnitDeployed;
  #endregion
}