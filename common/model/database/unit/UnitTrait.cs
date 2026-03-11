using System;
using System.Collections.Generic;
using System.Text.Json;
using Godot;
using MessagePack;

[Union(0, typeof(AttributeDefense))]
[Union(1, typeof(AttributeMovementPoints))]
[Union(2, typeof(AttributeStealth))]
[Union(3, typeof(AttributeCost))]
[Union(4, typeof(AttributeVisionRange))]
[Union(5, typeof(ActionMeleeAttack))]
[Union(6, typeof(ActionRangedAttack))]
public interface UnitTrait {
  public string Trait { get; }
  public TraitType Type { get; }
  public static UnitTrait FromJson(JsonElement json) {
    var traitId = json.GetProperty("id").GetString() ?? throw new JsonException($"Unit trait missing 'id' property");
    return traitId switch {
      "ATTRIBUTE.DEFENSE" => AttributeDefense.FromJson(json),
      "ATTRIBUTE.MOVEMENT_POINTS" => AttributeMovementPoints.FromJson(json),
      "ATTRIBUTE.STEALTH" => AttributeStealth.FromJson(json),
      "ATTRIBUTE.COST" => AttributeCost.FromJson(json),
      "ATTRIBUTE.VISION_RANGE" => AttributeVisionRange.FromJson(json),
      "ACTION.MELEE_ATTACK" => ActionMeleeAttack.FromJson(json),
      "ACTION.RANGED_ATTACK" => ActionRangedAttack.FromJson(json),
      _ => throw new JsonException($"Unknown trait type: {traitId}")
    };
  }
}

public enum TraitType {
  ATTRIBUTE,
  ACTION,
  EFFECT
}

#region Attributes
[MessagePackObject]
public record AttributeDefense : UnitTrait {
  [IgnoreMember] public string Trait => "ATTRIBUTE.DEFENSE";
  [IgnoreMember] public TraitType Type => TraitType.ATTRIBUTE;
  [Key(0)] public int Value { init; get; }

  public static AttributeDefense FromJson(JsonElement json) {
    try {
      return new AttributeDefense {
        Value = json.GetProperty("value").GetInt32()
      };
    } catch (JsonException ex) {
      throw new JsonException($"Failed to parse AttributeDefense from JSON: {ex.Message}", ex);
    }
  }
}

[MessagePackObject]
public record AttributeMovementPoints : UnitTrait {
  [IgnoreMember] public string Trait => "ATTRIBUTE.MOVEMENT_POINTS";
  [IgnoreMember] public TraitType Type => TraitType.ATTRIBUTE;
  [Key(0)] public int Value { init; get; }

  public static AttributeMovementPoints FromJson(JsonElement json) {
    try {
      return new AttributeMovementPoints {
        Value = json.GetProperty("value").GetInt32()
      };
    } catch (JsonException ex) {
      throw new JsonException($"Failed to parse AttributeMovementPoints from JSON: {ex.Message}", ex);
    }
  }
}

[MessagePackObject]
public record AttributeStealth : UnitTrait {
  [IgnoreMember] public string Trait => "ATTRIBUTE.STEALTH";
  [IgnoreMember] public TraitType Type => TraitType.ATTRIBUTE;
  [Key(0)] public int Value { init; get; }

  public static AttributeStealth FromJson(JsonElement json) {
    try {
      return new AttributeStealth {
        Value = json.GetProperty("value").GetInt32()
      };
    } catch (JsonException ex) {
      throw new JsonException($"Failed to parse AttributeStealth from JSON: {ex.Message}", ex);
    }
  }
}

[MessagePackObject]
public record AttributeCost : UnitTrait {
  [IgnoreMember] public string Trait => "ATTRIBUTE.COST";
  [IgnoreMember] public TraitType Type => TraitType.ATTRIBUTE;
  [Key(0)] public int Value { init; get; }

  public static AttributeCost FromJson(JsonElement json) {
    try {
      return new AttributeCost {
        Value = json.GetProperty("value").GetInt32()
      };
    } catch (JsonException ex) {
      throw new JsonException($"Failed to parse AttributeCost from JSON: {ex.Message}", ex);
    }
  }
}

[MessagePackObject]
public record AttributeVisionRange : UnitTrait {
  [IgnoreMember] public string Trait => "ATTRIBUTE.VISION_RANGE";
  [IgnoreMember] public TraitType Type => TraitType.ATTRIBUTE;
  [Key(0)] public int Value { init; get; }

  public static AttributeVisionRange FromJson(JsonElement json) {
    try {
      return new AttributeVisionRange {
        Value = json.GetProperty("value").GetInt32()
      };
    } catch (JsonException ex) {
      throw new JsonException($"Failed to parse AttributeVisionRange from JSON: {ex.Message}", ex);
    }
  }
}

#endregion

#region Actions
[MessagePackObject]
public record ActionMeleeAttack : UnitTrait {
  [IgnoreMember] public string Trait => "ACTION.MELEE_ATTACK";
  [IgnoreMember] public TraitType Type => TraitType.ACTION;
  [Key(0)] public string? CustomName { init; get; }
  [Key(1)] public int Strength { init; get; }

  public static ActionMeleeAttack FromJson(JsonElement json) {
    try {
      return new ActionMeleeAttack {
        CustomName = json.GetPropertyOrDefault<string?>("custom_name", null),
        Strength = json.GetProperty("strength").GetInt32()
      };
    } catch (JsonException ex) {
      throw new JsonException($"Failed to parse ActionMeleeAttack from JSON: {ex.Message}", ex);
    }
  }
}

[MessagePackObject]
public record ActionRangedAttack : UnitTrait {
  [IgnoreMember] public string Trait => "ACTION.RANGED_ATTACK";
  [IgnoreMember] public TraitType Type => TraitType.ACTION;
  [Key(0)] public string? CustomName { init; get; }
  [Key(1)] public int Strength { init; get; }
  [Key(2)] public int Cost { init; get; }
  [Key(3)] public int Range { init; get; }
  [Key(4)] public bool IgnoreTerrain { init; get; }

  public static ActionRangedAttack FromJson(JsonElement json) {
    try {
      return new ActionRangedAttack {
        CustomName = json.GetPropertyOrDefault<string?>("custom_name", null),
        Strength = json.GetProperty("strength").GetInt32(),
        Cost = json.GetPropertyOrDefault<int>("cost", 1),
        Range = json.GetProperty("range").GetInt32(),
        IgnoreTerrain = json.GetPropertyOrDefault<bool>("ignore_terrain", false)
      };
    } catch (Exception ex) {
      throw new Exception($"Failed to parse ActionRangedAttack from JSON: {ex.Message}", ex);
    }
  }
}
#endregion

#region Effects

#endregion

