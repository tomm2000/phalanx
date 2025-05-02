using System;
using Godot;

public partial class PlayerValue<T> : Node {
  public T Value { get; set; }
  private PlayerDataInterface _playerDataInterface = default!;

  public PlayerValue(T defaultValue, PlayerDataInterface playerDataInterface, string name) {
    _playerDataInterface = playerDataInterface;
    Value = defaultValue;
    Name = name;

    _playerDataInterface.AddChild(this, forceReadableName: true);
  }

  public override void _Ready() {
    _playerDataInterface.SyncPlayer += SERVER_SyncPlayer;
  }

  public delegate void OnValueChanged(T oldValue, T newValue);
  public event OnValueChanged? ValueChanged;


  public static implicit operator T(PlayerValue<T> serverValue) => serverValue.Value;

  public void SERVER_SetValue(T value) {
    if (!MultiplayerManager.IsHost) { throw new InvalidOperationException("Only the host can set server values."); }

    RpcId(_playerDataInterface.ClientPeerId, nameof(CLIENT_SetValue), value.Serialize());
  }

  private void SERVER_SyncPlayer() {
    if (!MultiplayerManager.IsHost) { throw new InvalidOperationException("Only the host can sync server values."); }

    RpcId(_playerDataInterface.ClientPeerId, nameof(CLIENT_SetValue), Value.Serialize());
  }

  [Rpc(
    mode: MultiplayerApi.RpcMode.Authority,
    CallLocal = true,
    TransferMode = MultiplayerPeer.TransferModeEnum.Reliable
  )]
  private void CLIENT_SetValue(byte[] serializedValue) {
    var newValue = serializedValue.Deserialize<T>();
    
    if (TlibGeneric.EqualsNullable(Value, newValue)) { return; }

    var oldValue = Value;
    Value = newValue;

    ValueChanged?.Invoke(oldValue, newValue);
  }
}