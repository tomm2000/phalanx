using System.Collections.Generic;
using MessagePack;

[MessagePackObject]
public struct Scenario {
  [Key(0)] public string Id { get; set; }
  [Key(1)] public string Name { get; set; }
  [Key(2)] public string Description { get; set; }
  [Key(3)] public IReadOnlyDictionary<DatabaseEntryString, MapData> Maps { get; set; }
  [Key(4)] public IReadOnlyDictionary<DatabaseEntryString, UnitBlueprint> Units { get; set; }
}