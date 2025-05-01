using System;
using System.Collections.Generic;
using Godot;
using MessagePack;
using MessagePack.Resolvers;

public static class Serialization {
  public static byte[] Serialize<T>(this T obj) {
    try {
      return MessagePackSerializer.Serialize(obj, StandardResolverAllowPrivate.Options);
    } catch (Exception e) {
      GD.PrintErr($"=======================\nFailed to pack data: {e}");
      return [];
    }
  }

  public static T Deserialize<T>(this byte[] data) {
    try {
      return MessagePackSerializer.Deserialize<T>(data, StandardResolverAllowPrivate.Options);
    } catch (Exception e) {
      GD.PrintErr($"=======================\nFailed to unpack data: {e}");
      return default!;
    }
  }

  public static Godot.Collections.Dictionary<K, V> ToGodotDictionary<[MustBeVariant] K, [MustBeVariant] V>(this IReadOnlyDictionary<K, V> dict) where K : notnull {
    var result = new Godot.Collections.Dictionary<K, V>();
    foreach (var kvp in dict) {
      result.Add(kvp.Key, kvp.Value);
    }
    return result;
  }
}