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

public interface INetVar {
  void ApplyUpdateValue(byte[] newValue);
  void ApplySyncValue(byte[] newValue);
  void ResetToDefault();
  void SERVER_Sync(PeerID targetPeer);
}

public interface INetCollection : INetVar {
  void ApplyUpdateCollectionElement(byte[] key, byte[] newValue);
  void ApplyRemoveCollectionElement(byte[] key);
  void ApplyAddCollectionElement(byte[] key, byte[] newValue);
}


public class NetVar<T> : INetVar {
  public event Action<T, T>? OnValueChanged;

  public T Value { get; private set; }
  private readonly T _defaultValue;
  private readonly string _id;
  private NetStateManager _manager = default!;

  public NetVar(string uniqueId, T initialValue) {
    _id = uniqueId;
    Value = initialValue;
    _defaultValue = initialValue;
  }

  public void LinkManager(NetStateManager manager) {
    _manager = manager;
    _manager.RegisterVariable(_id, this);
  }

  public void ResetToDefault() {
    var oldValue = Value;
    Value = _defaultValue;
    OnValueChanged?.Invoke(oldValue, _defaultValue);
  }

  #region Server Update Handlers
  public void ApplyUpdateValue(byte[] newValue) {
    var deserializedValue = newValue.Deserialize<T>();
    var oldValue = Value;
    Value = deserializedValue;
    OnValueChanged?.Invoke(oldValue, deserializedValue);
  }

  public void ApplySyncValue(byte[] newValue) => ApplyUpdateValue(newValue);
  #endregion

  #region Update Methods
  public void SERVER_Sync(PeerID targetPeer = -1) {
    if (!MultiplayerManager.IsHost) { throw new InvalidOperationException("Only the host can sync server values."); }

    _manager.SERVER_SyncVariable(_id, Value, targetPeer);
  }

  public void SERVER_SetValue(T newValue) {
    if (!MultiplayerManager.IsHost) { throw new InvalidOperationException("Only the host can set server values."); }

    _manager.SERVER_UpdateVariable(_id, newValue);
  }
  #endregion
}

public class NetDictionary<TKey, TValue> :
  INetCollection,
  IEnumerable<KeyValuePair<TKey, TValue>>,
  IReadOnlyDictionary<TKey, TValue>
  where TKey : notnull {
  public event Action<TKey, TValue, TValue>? OnKeyUpdated;
  public event Action<TKey, TValue>? OnKeyAdded;
  public event Action<TKey, TValue>? OnKeyRemoved;
  public event Action? OnValueChanged;

  private readonly Dictionary<TKey, TValue> _dictionary = [];
  private readonly Dictionary<TKey, TValue> _defaultDictionary = [];
  private readonly string _id;
  private NetStateManager _manager = default!;

  public NetDictionary(string uniqueId, Dictionary<TKey, TValue>? initialDict = null) {
    _id = uniqueId;
    if (initialDict != null) {
      _dictionary = initialDict;
      _defaultDictionary = new Dictionary<TKey, TValue>(initialDict);
    }
  }

  public void LinkManager(NetStateManager manager) {
    _manager = manager;
    _manager.RegisterVariable(_id, this);
  }

  public void ResetToDefault() {
    _dictionary.Clear();
    foreach (var kvp in _defaultDictionary) {
      _dictionary[kvp.Key] = kvp.Value;
    }
    OnValueChanged?.Invoke();
  }

  #region Server Update Handlers
  public void ApplyUpdateValue(byte[] newValue) {
    var deserializedDict = newValue.Deserialize<Dictionary<TKey, TValue>>();

    // For simplicity, we'll just replace the entire dictionary on sync.
    _dictionary.Clear();
    foreach (var kvp in deserializedDict) {
      _dictionary[kvp.Key] = kvp.Value;
    }

    OnValueChanged?.Invoke();
  }

  public void ApplySyncValue(byte[] newValue) => ApplyUpdateValue(newValue);

  public void ApplyUpdateCollectionElement(byte[] key, byte[] newValue) {
    var deserializedKey = key.Deserialize<TKey>();
    var deserializedValue = newValue.Deserialize<TValue>();

    if (_dictionary.TryGetValue(deserializedKey, out var oldValue)) {
      _dictionary[deserializedKey] = deserializedValue;
      OnKeyUpdated?.Invoke(deserializedKey, oldValue, deserializedValue);
      OnValueChanged?.Invoke();
    } else {
      ApplyAddCollectionElement(key, newValue);
    }
  }

  public void ApplyAddCollectionElement(byte[] key, byte[] newValue) {
    var deserializedKey = key.Deserialize<TKey>();
    var deserializedValue = newValue.Deserialize<TValue>();

    if (_dictionary.ContainsKey(deserializedKey)) {
      GD.PushError($"[NetDictionary] Attempted to add already existing key '{deserializedKey}' in variable '{_id}'!");
      return;
    }

    _dictionary[deserializedKey] = deserializedValue;
    OnKeyAdded?.Invoke(deserializedKey, deserializedValue);
    OnValueChanged?.Invoke();
  }

  public void ApplyRemoveCollectionElement(byte[] key) {
    var deserializedKey = key.Deserialize<TKey>();

    if (_dictionary.TryGetValue(deserializedKey, out var oldValue)) {
      _dictionary.Remove(deserializedKey);
      OnKeyRemoved?.Invoke(deserializedKey, oldValue);
      OnValueChanged?.Invoke();
    } else {
      GD.PushError($"[NetDictionary] Attempted to remove non-existent key '{deserializedKey}' in variable '{_id}'!");
    }
  }
  #endregion

  #region Update Methods
  public void SERVER_Sync(PeerID targetPeer = -1) {
    if (!MultiplayerManager.IsHost) { throw new InvalidOperationException("Only the host can sync server values."); }

    _manager.SERVER_SyncVariable(_id, _dictionary, targetPeer);
  }

  public void SERVER_SetValue(Dictionary<TKey, TValue> newDict) {
    if (!MultiplayerManager.IsHost) { throw new InvalidOperationException("Only the host can set server values."); }

    _manager.SERVER_UpdateVariable(_id, newDict);
  }

  public void SERVER_SetKey(TKey key, TValue value) {
    if (!MultiplayerManager.IsHost) { throw new InvalidOperationException("Only the host can set server values."); }

    _manager.SERVER_UpdateCollectionElement(_id, key.Serialize(), value);
  }

  public void SERVER_AddKey(TKey key, TValue value) {
    if (!MultiplayerManager.IsHost) { throw new InvalidOperationException("Only the host can set server values."); }

    _manager.SERVER_AddCollectionElement(_id, key.Serialize(), value);
  }

  public void SERVER_RemoveKey(TKey key) {
    if (!MultiplayerManager.IsHost) { throw new InvalidOperationException("Only the host can set server values."); }

    _manager.SERVER_RemoveCollectionElement(_id, key.Serialize());
  }
  #endregion

  #region Accessors
  public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator() => _dictionary.GetEnumerator();
  System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => _dictionary.GetEnumerator();

  public bool ContainsKey(TKey key) => _dictionary.ContainsKey(key);

  public bool TryGetValue(TKey key, [MaybeNullWhen(false)] out TValue value) => _dictionary.TryGetValue(key, out value);

  public IEnumerable<TKey> Keys => _dictionary.Keys;

  public IEnumerable<TValue> Values => _dictionary.Values;

  public int Count => _dictionary.Count;

  public TValue this[TKey key] => _dictionary[key];
  #endregion
}


public class NetList<T> : INetCollection, IEnumerable<T>, IReadOnlyList<T> {
  public event Action<int, T, T>? OnIndexUpdated;
  public event Action<int, T>? OnIndexAdded;
  public event Action<int, T>? OnIndexRemoved;
  public event Action? OnValueChanged;

  private readonly List<T> _list = [];
  private readonly List<T> _defaultList = [];
  private readonly string _id;
  private NetStateManager _manager = default!;

  public NetList(string uniqueId, List<T>? initialList = null) {
    _id = uniqueId;
    if (initialList != null) {
      _list = initialList;
      _defaultList = [.. initialList];
    }
  }

  public void LinkManager(NetStateManager manager) {
    _manager = manager;
    _manager.RegisterVariable(_id, this);
  }

  public void ResetToDefault() {
    _list.Clear();
    _list.AddRange(_defaultList);
    OnValueChanged?.Invoke();
  }

  #region Server Update Handlers
  public void ApplyUpdateValue(byte[] newValue) {
    var deserializedList = newValue.Deserialize<List<T>>();

    // For simplicity, we'll just replace the entire list on sync.
    _list.Clear();
    _list.AddRange(deserializedList);

    OnValueChanged?.Invoke();
  }

  public void ApplySyncValue(byte[] newValue) => ApplyUpdateValue(newValue);

  public void ApplyUpdateCollectionElement(byte[] key, byte[] newValue) {
    var index = BitConverter.ToInt32(key, 0);
    var deserializedValue = newValue.Deserialize<T>();

    if (index >= 0 && index < _list.Count) {
      var oldValue = _list[index];
      _list[index] = deserializedValue;
      OnIndexUpdated?.Invoke(index, oldValue, deserializedValue);
      OnValueChanged?.Invoke();
    } else {
      GD.PushError($"[NetList] Attempted to update index '{index}' out of bounds in variable '{_id}'!");
    }
  }

  public void ApplyRemoveCollectionElement(byte[] key) {
    var index = BitConverter.ToInt32(key, 0);

    if (index >= 0 && index < _list.Count) {
      var oldValue = _list[index];
      _list.RemoveAt(index);
      OnIndexRemoved?.Invoke(index, oldValue);
      OnValueChanged?.Invoke();
    } else {
      GD.PushError($"[NetList] Attempted to remove index '{index}' out of bounds in variable '{_id}'!");
    }
  }

  public void ApplyAddCollectionElement(byte[] key, byte[] newValue) {
    var index = BitConverter.ToInt32(key, 0);
    var deserializedValue = newValue.Deserialize<T>();

    if (index == _list.Count) {
      _list.Add(deserializedValue);
      OnIndexAdded?.Invoke(index, deserializedValue);
      OnValueChanged?.Invoke();
    } else {
      GD.PushError($"[NetList] Attempted to add index '{index}' that is not at the end of the list in variable '{_id}'!");
    }
  }
  #endregion

  #region Update Methods
  public void SERVER_Sync(PeerID targetPeer = -1) {
    if (!MultiplayerManager.IsHost) { throw new InvalidOperationException("Only the host can sync server values."); }

    _manager.SERVER_SyncVariable(_id, _list, targetPeer);
  }

  public void SERVER_SetValue(List<T> newList) {
    if (!MultiplayerManager.IsHost) { throw new InvalidOperationException("Only the host can set server values."); }

    _manager.SERVER_UpdateVariable(_id, newList);
  }

  public void SERVER_SetIndex(int index, T value) {
    if (!MultiplayerManager.IsHost) { throw new InvalidOperationException("Only the host can set server values."); }

    _manager.SERVER_UpdateCollectionElement(_id, BitConverter.GetBytes(index), value.Serialize());
  }

  public void SERVER_Append(T value) {
    if (!MultiplayerManager.IsHost) { throw new InvalidOperationException("Only the host can set server values."); }

    _manager.SERVER_AddCollectionElement(_id, BitConverter.GetBytes(_list.Count), value.Serialize());
  }

  public void SERVER_RemoveAt(int index) {
    if (!MultiplayerManager.IsHost) { throw new InvalidOperationException("Only the host can set server values."); }

    _manager.SERVER_RemoveCollectionElement(_id, BitConverter.GetBytes(index));
  }

  public void SERVER_Pop() {
    if (!MultiplayerManager.IsHost) { throw new InvalidOperationException("Only the host can set server values."); }

    if (_list.Count > 0) {
      _manager.SERVER_RemoveCollectionElement(_id, BitConverter.GetBytes(_list.Count - 1));
    }
  }
  #endregion

  #region Accessors
  public IEnumerator<T> GetEnumerator() => _list.GetEnumerator();
  System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => _list.GetEnumerator();

  public int Count => _list.Count;

  public T this[int index] => _list[index];
  #endregion
}
