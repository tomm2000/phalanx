using System.Text.Json;

public static class JsonUtils {
  public static T GetPropertyOrDefault<T>(this JsonElement json, string propertyName, T defaultValue) {
    if (json.TryGetProperty(propertyName, out var property)) {
      return JsonSerializer.Deserialize<T>(property.GetRawText())!;
    }
    return defaultValue;
  }
}