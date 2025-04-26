using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Chickensoft.GodotNodeInterfaces;
using FluentResults;
using Godot;
using MessagePack;
using MessagePack.Resolvers;

public partial class ObservableProperty<T> {
  private T _value;

  public T Value {
    get { return _value; }
    set {
      if (_value!.Equals(value)) { return; }

      T oldValue = _value;
      _value = value;
      OnChange?.Invoke(oldValue, value);
    }
  }

  public event Action<T, T>? OnChange;

  public ObservableProperty(T value) {
    _value = value;
  }

  public static implicit operator T(ObservableProperty<T> property) {
    return property.Value;
  }

  public static implicit operator ObservableProperty<T>(T value) {
    return new ObservableProperty<T>(value);
  }

  public static bool operator ==(ObservableProperty<T>? a, ObservableProperty<T>? b) {
    if (a is null && b is null) {
      return true;
    } else if (a is null || b is null) {
      return false;
    } else {
      return a!.Value!.Equals(b!.Value);
    }
  }

  public static bool operator !=(ObservableProperty<T>? a, ObservableProperty<T>? b) {
    return !(a == b);
  }
  public override bool Equals(object? obj) {
    if (obj is T) {
      return Value!.Equals(obj);
    } else if (obj is ObservableProperty<T>) {
      return Value!.Equals((obj as ObservableProperty<T>)!.Value);
    } else {
      return false;
    }
  }

  public override int GetHashCode() => Value!.GetHashCode();

  public override string ToString() {
    if (Value == null) {
      return "";
    } else if (Value.ToString() == null) {
      return "";
    } else {
      return Value.ToString()!;
    }
  }
}