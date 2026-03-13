global using PeerID = long;
global using SteamID = ulong;
global using ClientID = string;
global using DatabaseEntryString = string;
global using UnitInstanceID = string;

public static partial class Constants {
  // Terrain
  public const float TERRAIN_SCALE = 1f;
  public const uint TERRAIN_MESH_RESOLUTION = 16; // should be at least 2, otherwise not enough details
  public const float SLOPE_STEEPNESS = .8f; // 0 to 1
  public const float HEIGHT_SCALE = .1f;
  public const float RIVER_HEIGHT_SCALE = 0.08f;

  public const string DATABASE_PATH = "res://database/";
  public const string DATABASE_ID_REGEX = @"[a-z0-9]+:([a-z0-9]+\.)*([a-z0-9]+)";

  [System.Text.RegularExpressions.GeneratedRegex(DATABASE_ID_REGEX)]
  public static partial System.Text.RegularExpressions.Regex DatabaseIdRegex();
}