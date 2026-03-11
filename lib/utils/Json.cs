using System.Text.Json;
using Godot;

public static class JsonUtils {
  public static T GetPropertyOrDefault<T>(this JsonElement json, string propertyName, T defaultValue) {
    if (json.TryGetProperty(propertyName, out var property)) {
      return property.Deserialize<T>()!;
    }
    return defaultValue;
  }
}