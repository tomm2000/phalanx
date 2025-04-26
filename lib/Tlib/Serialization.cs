using System;
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
}