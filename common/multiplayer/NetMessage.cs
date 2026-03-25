using System;
using System.Collections.Generic;
using Godot;
using Tlib.Serialization;

public interface INetMessage {
  string UniqueId { get; }
  ClientID ClientId { get; }
  MessageType MessageType { get; }
  void ReceiveMessage(byte[] payload, ClientID senderClientId, PeerID senderPeerId);
}

/// <summary>
/// A message that can be sent from the server to clients. The message will be sent to all Peers in the client list, and only 
/// triggered for the client with the matching ClientID filter.
/// </summary>
class ServerToClientMessage<T> : INetMessage {
  public event Action<T>? CLIENT_OnMessage;

  public ClientID ClientId { get; init; }
  public MessageType MessageType { get; init;} = MessageType.ServerToClient;
  public string UniqueId { get; init; }

  private NetMessageManager? _manager = null;

  public ServerToClientMessage(string uniqueId, ClientID clientFilter) {
    UniqueId = uniqueId;
    ClientId = clientFilter;
  }

  public ServerToClientMessage<T> Register(NetMessageManager manager) {
    _manager = manager;
    _manager.RegisterMessage(this);
    return this;
  }

  public void SERVER_Send(T payload, List<Client>? targetClients = null) {
    if (_manager == null) {
      GD.PushError($"[NetEvent] Attempting to send event '{UniqueId}' to clients, but event is not registered");
      return;
    }

    _manager.SendServerToClient(this, payload, targetClients);
  }

  public void ReceiveMessage(byte[] payload, ClientID clientFilter, PeerID senderPeerId) {
    if (ClientId != clientFilter) return; // Only trigger the event for the client with the matching ClientID filter

    CLIENT_OnMessage?.Invoke(payload.Deserialize<T>());
  }
}

class ClientToServerMessage<T> : INetMessage {
  public event Action<T, ClientID, PeerID>? SERVER_OnMessage;

  public ClientID ClientId { get; init; }
  public MessageType MessageType { get; init;} = MessageType.ClientToServer;
  public string UniqueId { get; init; }

  private NetMessageManager? _manager = null;

  public ClientToServerMessage(string uniqueId, ClientID clientFilter) {
    UniqueId = uniqueId;
    ClientId = clientFilter;
  }

  public ClientToServerMessage<T> LinkManager(NetMessageManager manager) {
    _manager = manager;
    _manager.RegisterMessage(this);
    return this;
  }

  public void CLIENT_Send(T payload) {
    if (_manager == null) {
      GD.PushError($"[NetEvent] Attempting to send event '{UniqueId}' to server, but event is not registered");
      return;
    }
    
    _manager.SendClientToServer(this, payload);
  }

  public void ReceiveMessage(byte[] payload, ClientID senderClientId, PeerID senderPeerId) {
    SERVER_OnMessage?.Invoke(payload.Deserialize<T>(), senderClientId, senderPeerId);
  }
}