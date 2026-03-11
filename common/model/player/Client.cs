using System;
using System.Collections.Generic;
using System.Linq;
using FluentResults;
using MessagePack;

public enum ConnectionStatus {
  Connected,
  Disconnected,
}

public enum ClientType {
  Human,
  Bot,
  Spectator,
}

[MessagePackObject]
public readonly struct Client(
  string uid,
  string name,
  PeerID peerId,
  ConnectionStatus connectionStatus,
  long joinTime,
  ClientType clientType = ClientType.Human,
  SteamID? steamId = null
) {
  [Key(0)] public readonly string UID = uid;
  [Key(1)] public readonly string Name = name;
  [Key(2)] public readonly PeerID PeerId = peerId;
  [Key(3)] public readonly ConnectionStatus ConnectionStatus = connectionStatus;
  [Key(4)] public readonly long JoinTime = joinTime;
  [Key(5)] public readonly ClientType ClientType = clientType;
  [Key(6)] public readonly SteamID? SteamId = steamId;
}

public static class PlayerNewExtensions {
  public static Client With(
    this Client player,
    string? uid = null,
    string? name = null,
    PeerID? peerId = null,
    ConnectionStatus? connectionStatus = null,
    long? joinTime = null,
    ClientType? clientType = null,
    SteamID? steamId = null
  ) {
    return new Client(
      uid ?? player.UID,
      name ?? player.Name,
      peerId ?? player.PeerId,
      connectionStatus ?? player.ConnectionStatus,
      joinTime ?? player.JoinTime,
      clientType ?? player.ClientType,
      steamId ?? player.SteamId
    );
  }

  // ==================== Connection Status ====================
  public static IEnumerable<Client> Connected(this IEnumerable<Client> clients) {
    return clients.Where(player => player.ConnectionStatus == ConnectionStatus.Connected);
  }

  public static IEnumerable<Client> Disconnected(this IEnumerable<Client> clients) {
    return clients.Where(player => player.ConnectionStatus == ConnectionStatus.Disconnected);
  }

  // ==================== Mapping ====================
  public static IEnumerable<PeerID> PeerIds(this IEnumerable<Client> clients) {
    return clients.Select(player => player.PeerId);
  }

  public static IEnumerable<string> UIDs(this IEnumerable<Client> clients) {
    return clients.Select(player => player.UID);
  }

  // ==================== Finding ====================
  public static Result<Client> FindByName(this IEnumerable<Client> clients, string name) {
    return clients.FirstOrFailure(player => player.Name == name, $"Player not found with name: {name}");
  }

  public static Result<Client> FindByUID(this IEnumerable<Client> clients, string uid) {
    return clients.FirstOrFailure(player => player.UID == uid, $"Player not found with UID: {uid}");
  }

  public static Result<Client> FindByPeerID(this IEnumerable<Client> clients, PeerID peerId) {
    return clients.FirstOrFailure(player => player.PeerId == peerId, $"Player not found with PeerId: {peerId}");
  }

  public static Result<Client> FindBySteamID(this IEnumerable<Client> clients, SteamID steamId) {
    return clients.FirstOrFailure(player => player.SteamId == steamId, $"Player not found with SteamId: {steamId}");
  }

  public static Result<Client> Find(this IEnumerable<Client> clients, Func<Client, bool> predicate) {
    return clients.FirstOrFailure(predicate, "Player not found");
  }

  // ==================== Checking ====================
  public static bool Contains(this IEnumerable<Client> clients, string uid) {
    return clients.Any(player => player.UID == uid);
  }

  public static bool Contains(this IEnumerable<Client> clients, PeerID peerId) {
    return clients.Any(player => player.PeerId == peerId);
  }

  public static bool Contains(this IEnumerable<Client> clients, Func<Client, bool> predicate) {
    return clients.Any(predicate);
  }

  // ==================== Sorting ====================
  public static IEnumerable<Client> SortByJoinTime(this IEnumerable<Client> clients) {
    return clients.OrderBy(player => player.JoinTime);
  }
}