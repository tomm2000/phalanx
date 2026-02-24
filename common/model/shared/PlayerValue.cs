using System;
using System.Collections.Generic;
using Godot;
using Tlib.Serialization;

public partial class PlayerValue<T> : Node {
  private Dictionary<PeerID, T> ClientValues = [];

  public T Value { get; private set; }
  private SharedDataBase _sharedDataBase = default!;

  public PlayerValue(T defaultValue, SharedDataBase sharedDataBase, string name) {
    _sharedDataBase = sharedDataBase;
    Value = defaultValue;
    Name = name;

    _sharedDataBase.AddChild(this, forceReadableName: true);
  }

  public override void _Ready() {
    _sharedDataBase.SyncPeer += SERVER_SyncPeer;
  }

  public delegate void OnValueChanged(T oldValue, T newValue);
  public event OnValueChanged? ValueChanged;
  public void InvokeValueChanged(T oldValue, T newValue) => ValueChanged?.Invoke(oldValue, newValue);


  public static implicit operator T(PlayerValue<T> serverValue) => serverValue.Value;

  public void SERVER_SetPeerValue(PeerID peer, T value) {
    if (!MultiplayerManager.IsHost) { throw new InvalidOperationException("Only the host can set peer values."); }

    ClientValues[peer] = value;

    RpcId(peer, nameof(CLIENT_ValueSet), value.Serialize());
  }

  private void SERVER_SyncPeer(PeerID peer) {
    if (!MultiplayerManager.IsHost) { throw new InvalidOperationException("Only the host can sync server values."); }

    if (ClientValues.TryGetValue(peer, out var value)) {
      RpcId(peer, nameof(CLIENT_ValueSet), value.Serialize());
    } else {
      RpcId(peer, nameof(CLIENT_ValueSet), Value.Serialize());
    }
  }

  [Rpc(
    mode: MultiplayerApi.RpcMode.Authority,
    CallLocal = true,
    TransferMode = MultiplayerPeer.TransferModeEnum.Reliable
  )]
  private void CLIENT_ValueSet(byte[] serializedValue) {
    var newValue = serializedValue.Deserialize<T>();
    
    if (MiscUtils.EqualsNullable(Value, newValue)) { return; }

    var oldValue = Value;
    Value = newValue;

    ValueChanged?.Invoke(oldValue, newValue);
  }
}