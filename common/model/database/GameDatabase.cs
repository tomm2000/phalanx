using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Godot;

public readonly struct DatabaseEntryID {
  public string Domain { get; init; }
  public string Type { get; init; }
  public string Name { get; init; }

  public override readonly string ToString() => $"{Domain}:{Type}.{Name}";
  public static DatabaseEntryID Parse(string fullID) {
    var parts = fullID.Split(':');
    if (parts.Length != 2) throw new ArgumentException($"Invalid database ID: {fullID}, must be in format 'domain:type.name'");

    var domain = parts[0];
    var typeAndName = parts[1].Split('.');
    if (typeAndName.Length != 2) throw new ArgumentException($"Invalid database ID: {fullID}, must be in format 'domain:type.name'");

    return new DatabaseEntryID {
      Domain = domain,
      Type = typeAndName[0],
      Name = typeAndName[1]
    };
  }
}

public static class GameDatabase {
  private static string DOMAIN_FILE = "domain.json";
  private static string DATA_FILE = "data.json";
  private static string ICON_FILE = "icon.png";
  private static string MODEL_FILE = "model.tscn";
  private static string DATABASE_DIRECTORY = "database/";
  private static string UNIT_DIRECTORY = "unit/";
  private static string MAP_DIRECTORY = "map/";

  public static JsonDocument LoadDataEntry(string entryId) {
    var entryID = DatabaseEntryID.Parse(entryId);
    var basePath = "res://";
    
    // check res://database/{domain}/domain.json
    var domainPath = $"{basePath}{DATABASE_DIRECTORY}{entryID.Domain}/{DOMAIN_FILE}";
    if (!ResourceLoader.Exists(domainPath)) {
      // TODO: check for user://mods/domain...
      throw new NotImplementedException($"Domain file not found for domain '{entryID.Domain}' at path: {domainPath}");
    }

    var resourcePath = $"{basePath}{DATABASE_DIRECTORY}{entryID.Domain}/{entryID.Type}/{entryID.Name}/{DATA_FILE}";
    if (!ResourceLoader.Exists(resourcePath)) {
      throw new System.IO.FileNotFoundException($"Data file not found for entry '{entryId}' at path: {resourcePath}");
    }

    var file = FileAccess.Open(resourcePath, FileAccess.ModeFlags.Read);
    var jsonText = file.GetAsText();
    return JsonDocument.Parse(jsonText);
  }
}


// {
//   "id": "phalanx",
//   "name": "Phalanx",
//   "description": "The base domain for the phalanx game"
// }