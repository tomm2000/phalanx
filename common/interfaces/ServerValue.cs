using System;
using Godot;


public partial class ServerValue<T> : Node {
  public T Value { get; set; }
  private GameDataInterface _gameDataInterface = default!;

  public ServerValue(T defaultValue, GameDataInterface gameDataInterface, string name) {
    _gameDataInterface = gameDataInterface;
    Value = defaultValue;
    Name = name;

    _gameDataInterface.AddChild(this, forceReadableName: true);
  }

  public override void _Ready() {
    _gameDataInterface.SyncPlayer += (player) => { SERVER_SyncClient(player.PeerId); };
  }

  public delegate void OnValueChanged(T oldValue, T newValue);
  public event OnValueChanged? ValueChanged;


  public static implicit operator T(ServerValue<T> serverValue) => serverValue.Value;

  public void SERVER_SetValue(T value) {
    if (!MultiplayerManager.IsHost) { throw new InvalidOperationException("Only the host can set server values."); }

    Rpc(nameof(CLIENT_SetValue), value.Serialize());
  }

  private void SERVER_SyncClient(long peerId) {
    if (!MultiplayerManager.IsHost) { throw new InvalidOperationException("Only the host can sync server values."); }

    RpcId(peerId, nameof(CLIENT_SetValue), Value.Serialize());
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