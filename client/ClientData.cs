using System;
using Godot;
using Steam;
using Steamworks;

public static class ClientData {
  public static string Username { get; private set; }
  public static SteamId? SteamId { get; private set; } = null;

  static ClientData() {
    if (!SteamClient.IsValid) {
      Username = "Guest" + new Random().Next(1000, 9999).ToString();
      return;
    } else {
      Username = SteamClient.Name;
      SteamId = SteamClient.SteamId;
    }
  }
}