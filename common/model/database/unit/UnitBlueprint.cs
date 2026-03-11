using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using FluentResults;
using Godot;
using MessagePack;

[MessagePackObject]
public struct UnitBlueprint {
  [Key(0)] public required string Name { get; init; }
  [Key(1)] public required string ID { get; init; }
  [Key(2)] public required string Description { get; init; }
  [Key(3)] public required string Icon { get; init; }
  [Key(4)] public required string Model { get; init; }
  [Key(5)] public required IReadOnlyList<UnitTrait> Traits { get; init; }
  
  public readonly IEnumerable<UnitTrait> GetTraitsByID(string traitID) {
    for (int i = 0; i < Traits.Count; i++) {
      if (Traits[i].Trait == traitID) {
        yield return Traits[i];
      }
    }
  }

  public readonly IEnumerable<UnitTrait> GetTraitsOfType(TraitType traitType) {
    for (int i = 0; i < Traits.Count; i++) {
      if (Traits[i].Type == traitType) {
        yield return Traits[i];
      }
    }
  }

  public static UnitBlueprint FromJson(JsonDocument document) {
    var json = document.RootElement;
    var unitID = json.GetProperty("id").GetString() ?? throw new JsonException("Missing 'id' property");

    // load traits
    if (!json.TryGetProperty("traits", out var traitsProperty)) throw new JsonException($"Unit '{unitID}' is missing 'traits' property");
    var traits = traitsProperty.EnumerateArray()
      .Select(traitJson => UnitTrait.FromJson(traitJson))
      .ToList();

    // TODO: load icon

    // TODO: load model

    var unitBlueprint = new UnitBlueprint {
      ID = unitID,
      Name = json.GetProperty("name").GetString() ?? throw new JsonException($"Unit '{unitID}' is missing 'name' property"),
      Description = json.GetProperty("description").GetString() ?? throw new JsonException($"Unit '{unitID}' is missing 'description' property"),
      Icon = json.GetProperty("icon").GetString() ?? throw new JsonException($"Unit '{unitID}' is missing 'icon' property"),
      Model = json.GetProperty("model").GetString() ?? throw new JsonException($"Unit '{unitID}' is missing 'model' property"),
      Traits = traits
    };

    return unitBlueprint;
  }
}