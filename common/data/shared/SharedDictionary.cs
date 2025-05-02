using System;
using System.Collections.Generic;
using Godot;

public partial class SharedDictionary<K, V> : Node where K : notnull {
  private Dictionary<K, V> _value { get; set; } = [];
  public IReadOnlyDictionary<K, V> Value => _value;

  private SharedDataBase _sharedDataBase = default!;

  public SharedDictionary(Dictionary<K, V> defaultValues, SharedDataBase sharedDataBase, string name) {
    _sharedDataBase = sharedDataBase;
    _value = defaultValues;
    Name = name;

    _sharedDataBase.AddChild(this, forceReadableName: true);
  }

  public V this[K key] {
    get {
      if (_value.TryGetValue(key, out V? value)) {
        return value!;
      } else {
        throw new KeyNotFoundException($"Key {key} not found in SharedDictionary.");
      }
    }
    private set => SERVER_SetValue(key, value);
  }

  public SharedDictionary(V defaultValue, SharedDataBase dataInterface, string name) {
    _sharedDataBase.AddChild(this, forceReadableName: true);
  }

  public override void _Ready() {
    _sharedDataBase.SyncPeer += SERVER_SyncPeer;
  }

  public event Action? DictionaryChanged;

  public delegate void OnValueChanged(K key, V oldValue, V newValue);
  public event OnValueChanged? ValueChanged;

  public delegate void OnValueRemoved(K key, V oldValue);
  public event OnValueRemoved? ValueRemoved;

  public delegate void OnValueAdded(K key, V newValue);
  public event OnValueAdded? ValueAdded;

  public void SERVER_SetValue(K key, V value) {
    if (!MultiplayerManager.IsHost) { throw new InvalidOperationException("Only the host can set server values."); }

    Rpc(nameof(CLIENT_SetValue), key.Serialize(), value.Serialize());
  }

  public void SERVER_AddValue(K key, V value) {
    if (!MultiplayerManager.IsHost) { throw new InvalidOperationException("Only the host can set server values."); }

    Rpc(nameof(CLIENT_SetValue), key.Serialize(), value.Serialize());
  }

  public void SERVER_RemoveValue(K key) {
    if (!MultiplayerManager.IsHost) { throw new InvalidOperationException("Only the host can set server values."); }

    Rpc(nameof(CLIENT_RemoveValue), key.Serialize());
  }

  public void SERVER_SyncPeer(long peerId) {
    if (!MultiplayerManager.IsHost) { throw new InvalidOperationException("Only the host can sync server values."); }

    RpcId(peerId, nameof(CLIENT_SyncDictionary), _value.Serialize());
  }

  [Rpc(
    mode: MultiplayerApi.RpcMode.Authority,
    CallLocal = true,
    TransferMode = MultiplayerPeer.TransferModeEnum.Reliable
  )]
  private void CLIENT_SetValue(byte[] serializedKey, byte[] serializedValue) {
    var key = serializedKey.Deserialize<K>();
    var newValue = serializedValue.Deserialize<V>();

    if (key == null) {
      GD.PrintErr($"SharedDictionary: {nameof(key)} is null after deserialization. Key: {key}");
      return;
    }

    if (_value.TryGetValue(key, out V? oldValue)) {
      if (oldValue!.Equals(newValue)) { return; }

      _value[key] = newValue;
      ValueChanged?.Invoke(key, oldValue, newValue);
      DictionaryChanged?.Invoke();
    } else {
      _value.Add(key, newValue);
      ValueAdded?.Invoke(key, newValue);
      DictionaryChanged?.Invoke();
    }
  }

  [Rpc(
    mode: MultiplayerApi.RpcMode.Authority,
    CallLocal = true,
    TransferMode = MultiplayerPeer.TransferModeEnum.Reliable
  )]
  private void CLIENT_RemoveValue(byte[] serializedKey) {
    var key = serializedKey.Deserialize<K>();

    if (key == null) {
      GD.PrintErr($"SharedDictionary: {nameof(key)} is null after deserialization. Key: {key}");
      return;
    }

    if (_value.TryGetValue(key, out V? value)) {
      var oldValue = value;
      _value.Remove(key);
      ValueRemoved?.Invoke(key, oldValue);
      DictionaryChanged?.Invoke();
    }
  }

  [Rpc(
    mode: MultiplayerApi.RpcMode.Authority,
    CallLocal = true,
    TransferMode = MultiplayerPeer.TransferModeEnum.Reliable
  )]
  private void CLIENT_SyncDictionary(byte[] serializedDictionary) {
    var dictionary = serializedDictionary.Deserialize<Dictionary<K, V>>();

    if (dictionary == null) {
      GD.PrintErr($"SharedDictionary: {nameof(dictionary)} is null after deserialization. Dictionary: {dictionary}");
      return;
    }

    var changed = false;

    // remove all values that are not in the new dictionary
    foreach (var key in _value.Keys) {
      if (!dictionary.ContainsKey(key)) {
        var oldValue = _value[key];
        _value.Remove(key);
        ValueRemoved?.Invoke(key, oldValue);
        changed = true;
      }
    }

    // add all values that are in the new dictionary
    foreach (var kvp in dictionary) {
      var key = kvp.Key;
      var newValue = kvp.Value;

      if (_value.TryGetValue(key, out V? oldValue)) {
        if (oldValue!.Equals(newValue)) { continue; }

        _value[key] = newValue;
        ValueChanged?.Invoke(key, oldValue, newValue);
      } else {
        _value.Add(key, newValue);
        ValueAdded?.Invoke(key, newValue);
      }

      changed = true;
    }

    if (changed) {
      DictionaryChanged?.Invoke();
    }
  }
}