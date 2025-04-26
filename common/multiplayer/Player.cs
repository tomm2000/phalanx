using System;
using System.Collections.Generic;
using System.Linq;
using FluentResults;
using MessagePack;

public enum ConnectionStatus {
  Connected,
  Disconnected,
}

[MessagePackObject]
public readonly struct Player(
  string uid,
  string username,
  long peerId,
  ConnectionStatus connectionStatus,
  long joinTime,
  ulong? steamId = null
) {
  [Key(0)] public readonly string UID = uid;
  [Key(1)] public readonly string Username = username;
  [Key(2)] public readonly long PeerId = peerId;
  [Key(3)] public readonly ConnectionStatus ConnectionStatus = connectionStatus;
  [Key(4)] public readonly long JoinTime = joinTime;
  [Key(5)] public readonly ulong? SteamId = steamId;
}

public static class PlayerNewExtensions {
  public static Player With(
    this Player player,
    string? uid = null,
    string? username = null,
    long? peerId = null,
    ConnectionStatus? connectionStatus = null,
    long? joinTime = null,
    ulong? steamId = null
  ) {
    return new Player(
      uid ?? player.UID,
      username ?? player.Username,
      peerId ?? player.PeerId,
      connectionStatus ?? player.ConnectionStatus,
      joinTime ?? player.JoinTime,
      steamId ?? player.SteamId
    );
  }

  // ==================== Connection Status ====================
  public static IEnumerable<Player> Connected(this IEnumerable<Player> players) {
    return players.Where(player => player.ConnectionStatus == ConnectionStatus.Connected);
  }

  public static IEnumerable<Player> Disconnected(this IEnumerable<Player> players) {
    return players.Where(player => player.ConnectionStatus == ConnectionStatus.Disconnected);
  }

  // ==================== Mapping ====================
  public static IEnumerable<long> PeerIds(this IEnumerable<Player> players) {
    return players.Select(player => player.PeerId);
  }

  public static IEnumerable<string> UIDs(this IEnumerable<Player> players) {
    return players.Select(player => player.UID);
  }

  // ==================== Finding ====================
  public static Result<Player> FindByName(this IEnumerable<Player> players, string name) {
    return players.FirstOrFailure(player => player.Username == name, $"Player not found with name: {name}");
  }

  public static Result<Player> FindByUID(this IEnumerable<Player> players, string uid) {
    return players.FirstOrFailure(player => player.UID == uid, $"Player not found with UID: {uid}");
  }

  public static Result<Player> FindByPeerID(this IEnumerable<Player> players, long peerId) {
    return players.FirstOrFailure(player => player.PeerId == peerId, $"Player not found with PeerId: {peerId}");
  }

  public static Result<Player> FindBySteamID(this IEnumerable<Player> players, ulong steamId) {
    return players.FirstOrFailure(player => player.SteamId == steamId, $"Player not found with SteamId: {steamId}");
  }

  public static Result<Player> Find(this IEnumerable<Player> players, Func<Player, bool> predicate) {
    return players.FirstOrFailure(predicate, "Player not found");
  }

  // ==================== Checking ====================
  public static bool Contains(this IEnumerable<Player> players, string uid) {
    return players.Any(player => player.UID == uid);
  }

  public static bool Contains(this IEnumerable<Player> players, long peerId) {
    return players.Any(player => player.PeerId == peerId);
  }

  public static bool Contains(this IEnumerable<Player> players, Func<Player, bool> predicate) {
    return players.Any(predicate);
  }

  // ==================== Sorting ====================
  public static IEnumerable<Player> SortByJoinTime(this IEnumerable<Player> players) {
    return players.OrderBy(player => player.JoinTime);
  }
}