using System;
using Godot;
using GodotSteam;

public static class ClientData {
  public static string Username { get; private set; }
  public static ulong? SteamId { get; private set; } = null;

  static ClientData() {
    if (!Steam.IsSteamRunning()) {
      Username = "Guest" + new Random().Next(1000, 9999).ToString();
      return;
    } else {
      Username = Steam.GetPersonaName();
    }
  }
}