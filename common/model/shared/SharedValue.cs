using System;
using Godot;
using Tlib.Serialization;


public partial class SharedValue<T> : Node {
  public T Value { get; set; }
  private SharedDataBase _sharedDataBase = default!;

  public SharedValue(T defaultValue, SharedDataBase sharedDataBase, string name) {
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


  public static implicit operator T(SharedValue<T> serverValue) => serverValue.Value;

  public void SERVER_SetValue(T value) {
    if (!MultiplayerManager.IsHost) { throw new InvalidOperationException("Only the host can set server values."); }

    Rpc(nameof(CLIENT_ValueSet), value.Serialize());
  }

  private void SERVER_SyncPeer(PeerID peer) {
    if (!MultiplayerManager.IsHost) { throw new InvalidOperationException("Only the host can sync server values."); }

    RpcId(peer, nameof(CLIENT_ValueSet), Value.Serialize());
  }

  [Rpc(
    mode: MultiplayerApi.RpcMode.Authority,
    CallLocal = true,
    TransferMode = MultiplayerPeer.TransferModeEnum.Reliable
  )]
  private void CLIENT_ValueSet(byte[] serializedValue) {
    var newValue = serializedValue.Deserialize<T>();
    
    if (TlibGeneric.EqualsNullable(Value, newValue)) { return; }

    var oldValue = Value;
    Value = newValue;

    ValueChanged?.Invoke(oldValue, newValue);
  }
}