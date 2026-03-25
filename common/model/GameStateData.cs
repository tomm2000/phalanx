using System.Collections.Generic;
using MessagePack;

[MessagePackObject]
public class GameStateData {
  [Key(1)] public string GameId { get; set; } = System.Guid.NewGuid().ToString();
  [Key(2)] public Dictionary<ClientID, PlayerData> PlayerData { get; set; } = [];
  [Key(3)] public DatabaseStateData DatabaseStateData { get; set; } = new();
}

[MessagePackObject]
public class DatabaseStateData {
  [Key(1)] public string? SelectedScenarioID { get; set; } = null;
  [Key(2)] public string? SelectedMapID { get; set; } = null;
  [Key(3)] public List<string> ActiveDomains { get; set; } = [];
}

public enum GameStage {
  Disconnected,
  Lobby,
  Deployment,
  Battle
}