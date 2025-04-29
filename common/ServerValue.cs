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
    if (MultiplayerManager.MultiplayerStatus == MultiplayerStatus.Disconnected) { return; }

    if (!MultiplayerManager.IsHost) {
      CLIENT_RequestSync();
    }
  }

  public delegate void OnValueChanged(T oldValue, T newValue);
  public event OnValueChanged? ValueChanged;


  public static implicit operator T(ServerValue<T> serverValue) => serverValue.Value;

  public void SERVER_SetValue(T value) {
    if (!MultiplayerManager.IsHost) { throw new InvalidOperationException("Only the host can set server values."); }

    Rpc(nameof(CLIENT_SetValue), value.Serialize());
  }

  public void CLIENT_RequestSync() {
    if (MultiplayerManager.IsHost) { throw new InvalidOperationException("Only clients can request sync."); }

    RpcId(1, nameof(SERVER_ClientSyncRequest));
  }

  [Rpc(
    mode: MultiplayerApi.RpcMode.AnyPeer,
    CallLocal = false,
    TransferMode = MultiplayerPeer.TransferModeEnum.Reliable
  )]
  private void SERVER_ClientSyncRequest() {
    if (!MultiplayerManager.IsHost) { throw new InvalidOperationException("Only the host can sync server values."); }

    var peerId = MultiplayerManager.RpcSenderId();
    SERVER_SyncClient(peerId);
  }

  public void SERVER_SyncClient(int peerId) {
    if (!MultiplayerManager.IsHost) { throw new InvalidOperationException("Only the host can sync server values."); }

    RpcId(peerId, nameof(CLIENT_SetValue), Value.Serialize());
  }

  [Rpc(
    mode: MultiplayerApi.RpcMode.Authority,
    CallLocal = true,
    TransferMode = MultiplayerPeer.TransferModeEnum.Reliable
  )]
  private void CLIENT_SetValue(byte[] serializedValue) {
    var value = serializedValue.Deserialize<T>();

    if (value == null) {
      GD.PrintErr($"ServerValue: {nameof(Value)} is null after deserialization. Value: {Value}");
      return;
    }

    if (value.Equals(Value)) { return; }

    var oldValue = Value;
    Value = value;

    ValueChanged?.Invoke(oldValue, value);

    GD.Print($"ServerValue: {nameof(Value)} changed from {oldValue} to {value}");
  }
}