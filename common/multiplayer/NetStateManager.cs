using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Chickensoft.AutoInject;
using Chickensoft.Introspection;

using Godot;
using Steamworks;
using Tlib.NodeExt;
using Tlib.Serialization;

[Meta(typeof(IAutoConnect), typeof(IAutoNode))]
public partial class NetStateManager : Node {
  public override void _Notification(int what) => this.Notify(what);

  #region Nodes
  [Dependency] GameInstance GameInstance => this.DependOn<GameInstance>();
  #endregion

  private readonly Dictionary<string, INetVar> _networkVariables = [];

  public override void _Ready() {
  }

  public void OnResolved() {
    GameInstance.SERVER_SyncPeer += SERVER_SyncPeer;
  }

  public void RegisterVariable(string uniqueId, INetVar variable) {
    if (_networkVariables.ContainsKey(uniqueId)) {
      GD.PushError($"[NetStateManager] A variable with ID '{uniqueId}' is already registered!");
      return;
    }

    _networkVariables[uniqueId] = variable;
  }

  public void CLIENT_RequestSync() {
    if (!MultiplayerManager.IsHost) {
      RpcId(1, nameof(SERVER_Sync));
    }
  }

  [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = false, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
  private void SERVER_Sync() => SERVER_SyncPeer(MultiplayerManager.RpcSenderId());

  private void SERVER_SyncPeer(PeerID peerId) {
    if (!MultiplayerManager.IsHost) { return; }

    foreach (var kvp in _networkVariables) {
      var variable = kvp.Value;

      variable.SERVER_Sync(peerId);
    }
  }

  #region Validation
  private void ValidateVariableExists(string variableId, out INetVar outVariable) {
    if (!_networkVariables.TryGetValue(variableId, out var variable)) {
      throw new InvalidOperationException($"[NetStateManager] No variable registered with ID '{variableId}'!");
    }

    outVariable = variable;
  }

  private bool ValidateVariableIsCollection(INetVar variable, string variableId, out INetCollection outCollection) {
    if (variable is not INetCollection collectionVariable) {
      throw new InvalidOperationException($"[NetStateManager] Variable with ID '{variableId}' is not a collection variable!");
    }

    outCollection = collectionVariable;
    return true;
  }
  #endregion

  #region Variable Updates
  public void SERVER_SyncVariable<T>(string variableId, T value, PeerID targetPeer = -1) {
    if (!MultiplayerManager.IsHost) { throw new InvalidOperationException("Only the host can set server values."); }

    if (targetPeer == -1) {
      Rpc(nameof(RpcSyncVariable), variableId, value.Serialize());
    } else {
      RpcId(targetPeer, nameof(RpcSyncVariable), variableId, value.Serialize());
    }
  }

  [Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
  private void RpcSyncVariable(string variableId, byte[] serializedValue) {
    ValidateVariableExists(variableId, out var variable);

    variable.ApplySyncValue(serializedValue);
  }

  public void SERVER_UpdateVariable<T>(string variableId, T newValue) {
    if (!MultiplayerManager.IsHost) { throw new InvalidOperationException("Only the host can set server values."); }

    Rpc(nameof(RpcUpdateVariable), variableId, newValue.Serialize());
  }

  [Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
  private void RpcUpdateVariable(string variableId, byte[] newValue) {
    ValidateVariableExists(variableId, out var variable);

    variable.ApplyUpdateValue(newValue);
  }
  #endregion

  #region Collection Variable Updates
  // ==============================================================================================================================================

  public void SERVER_UpdateCollectionElement<T>(string variableId, byte[] key, T newValue) {
    if (!MultiplayerManager.IsHost) { throw new InvalidOperationException("Only the host can set server values."); }

    Rpc(nameof(RpcUpdateCollectionElement), variableId, key, newValue.Serialize());
  }

  [Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
  private void RpcUpdateCollectionElement(string variableId, byte[] key, byte[] newValue) {
    ValidateVariableExists(variableId, out var variable);
    ValidateVariableIsCollection(variable, variableId, out var collectionVariable);

    collectionVariable.ApplyUpdateCollectionElement(key, newValue);
  }

  // ==============================================================================================================================================

  public void SERVER_AddCollectionElement<T>(string variableId, byte[] key, T newValue) {
    if (!MultiplayerManager.IsHost) { throw new InvalidOperationException("Only the host can set server values."); }

    Rpc(nameof(RpcAddCollectionElement), variableId, key, newValue.Serialize());
  }

  [Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
  private void RpcAddCollectionElement(string variableId, byte[] key, byte[] newValue) {
    ValidateVariableExists(variableId, out var variable);
    ValidateVariableIsCollection(variable, variableId, out var collectionVariable);

    collectionVariable.ApplyAddCollectionElement(key, newValue);
  }

  // ==============================================================================================================================================

  public void SERVER_RemoveCollectionElement(string variableId, byte[] key) {
    if (!MultiplayerManager.IsHost) { throw new InvalidOperationException("Only the host can set server values."); }

    Rpc(nameof(RpcRemoveCollectionElement), variableId, key);
  }

  [Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
  private void RpcRemoveCollectionElement(string variableId, byte[] key) {
    ValidateVariableExists(variableId, out var variable);
    ValidateVariableIsCollection(variable, variableId, out var collectionVariable);

    collectionVariable.ApplyRemoveCollectionElement(key);
  }
  
  // ==============================================================================================================================================
  #endregion
}