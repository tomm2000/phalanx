using System;
using System.Collections.Generic;
using Chickensoft.AutoInject;
using Chickensoft.Introspection;
using Godot;
using System.Text.Json.Serialization;
using System.Text.Json;

[Meta(typeof(IAutoConnect), typeof(IAutoNode))]
public partial class ScenarioManager : Node {
  public override void _Notification(int what) => this.Notify(what);
  
  #region Nodes
  [Dependency] NetStateManager NetStateManager => this.DependOn<NetStateManager>();
  [Dependency] Main Main => this.DependOn<Main>();
  #endregion

  #region Properties
  public NetVar<string?> SelectedMapID { get; init; } = new("SelectedMapID", null);
  public NetVar<Scenario?> SelectedScenario { get; init; } = new("SelectedScenario", null);
  #endregion

  #region Events
  #endregion


  public void OnResolved() {
    SelectedMapID.LinkManager(NetStateManager);
    SelectedScenario.LinkManager(NetStateManager);

    // FIXME: temporary solution to load a default scenario on game start.
    Main.SERVER_NetworkingReady += () => LoadScenario("phalanx:scenario.standard");
  }

  public MapData GetSelectedMap() {
    if (SelectedScenario.Value == null) throw new InvalidOperationException("No selected scenario loaded.");
    if (SelectedMapID.Value == null) throw new InvalidOperationException("No map selected.");

    if (!SelectedScenario.Value.Value.Maps.TryGetValue(SelectedMapID.Value, out var mapData)) {
      throw new Exception($"Selected map ID '{SelectedMapID.Value}' not found in selected scenario, available maps: {string.Join(", ", SelectedScenario.Value.Value.Maps.Keys)}");
    }
    return mapData;
  }

  public void LoadScenario(string scenarioId) {
    if (!MultiplayerManager.IsHost) return; // Only the host should load scenarios, then sync to clients via NetVars

    Logger.Debug($"Loading scenario with ID: {scenarioId}");

    var scenarioJson = GameDatabase.LoadDataEntry(scenarioId) ?? throw new Exception($"Failed to find scenario entry for scenario ID: {scenarioId}");
    var scenarioData = JsonSerializer.Deserialize<ScenarioJson>(scenarioJson) ?? throw new Exception($"Failed to parse scenario JSON for scenario ID: {scenarioId}");
    
    // Load units
    var unitsListJson = scenarioData.Units;
    var units = new Dictionary<string, UnitBlueprint>();

    foreach (var unitId in unitsListJson) {
      var unitJson = GameDatabase.LoadDataEntry(unitId) ?? throw new Exception($"Failed to find unit entry for unit ID: {unitId}");
      var unitBlueprintResult = UnitBlueprint.FromJson(unitJson);
      var unitBlueprint = unitBlueprintResult;
      units[unitBlueprint.ID] = unitBlueprint;
    }

    // TODO: Load maps
    // var map = DevMap.GenerateMap(width: 19, height: 19, seed: 1);
    // var maps = new Dictionary<string, MapData> {
    //   { "phalanx:map.dev1", map }
    // };
    var mapListJson = scenarioData.Maps;
    var maps = new Dictionary<string, MapData>();

    foreach (var mapId in mapListJson) {
      Logger.Debug($"Loading map with ID: {mapId}");
      var mapJson = GameDatabase.LoadDataEntry(mapId) ?? throw new Exception($"Failed to find map entry for map ID: {mapId}");
      var mapDataResult = MapData.FromJson(mapJson);
      var mapData = mapDataResult;

      Logger.Debug($"Loaded map with ID '{mapId}': {mapData.mapName} - {mapData.mapDescription}");
      maps[mapData.mapId] = mapData;
    }

    // TODO: Validate that all units and maps referenced in the scenario are successfully loaded before adding the scenario to the manager

    // compose scenario
    var scenario = new Scenario {
      Id = scenarioData.Id,
      Name = scenarioData.Name,
      Description = scenarioData.Description,
      Maps = maps,
      Units = units
    };

    SelectedScenario.SERVER_SetValue(scenario);
    SelectedMapID.SERVER_SetValue(scenarioData.Maps[0]); // TODO: this should probably be handled by the lobby menu instead, and not just arbitrarily select the first map in the scenario
  }
}

record ScenarioJson(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("description")] string Description,
    [property: JsonPropertyName("maps")] List<string> Maps,
    [property: JsonPropertyName("units")] List<string> Units
);