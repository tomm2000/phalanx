using System;
using System.Collections.Generic;
using Godot;
using Tlib.Serialization;

public interface IServerToClientMessage {
  string UniqueId { get; }
  ClientID ClientId { get; }
  void ReceiveMessage(byte[] payload, PeerID senderPeerId, ClientID? clientFilter = null);
}

public class ServerToClientMessage<T>(string uniqueId, ClientID clientID) : IServerToClientMessage {
  public delegate void ClientMessageHandler(T payload);
  public event ClientMessageHandler? CLIENT_OnMessage;

  public ClientID ClientId { get; init; } = clientID;
  public string UniqueId { get; init; } = uniqueId;

  private NetMessageManager? _manager = null;

  public ServerToClientMessage<T> LinkManager(NetMessageManager manager) {
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

  public void ReceiveMessage(byte[] payload, PeerID senderPeerId, ClientID? clientFilter = null) {
    // Only trigger the event for the client with the matching ClientID filter
    // This should only happen when the server is sending a message to itself (for example sending a message to a bot client), in other
    // cases the RPC is not even called on unintended clients
    if (ClientId != clientFilter && clientFilter != null) return;

    CLIENT_OnMessage?.Invoke(payload.Deserialize<T>());
  }
}



public interface IClientToServerMessage {
  string UniqueId { get; }
  void ReceiveMessage(byte[] payload, PeerID senderPeerId, ClientID senderClientId);
}

public class ClientToServerMessage<T>(string uniqueId) : IClientToServerMessage {
  public delegate void ServerMessageHandler(T payload, ClientID senderClientId, PeerID senderPeerId);
  public event ServerMessageHandler? SERVER_OnMessage;

  public string UniqueId { get; init; } = uniqueId;

  private NetMessageManager? _manager = null;

  public ClientToServerMessage<T> LinkManager(NetMessageManager manager) {
    _manager = manager;
    _manager.RegisterMessage(this);
    return this;
  }

  public void CLIENT_Send(T payload, ClientID clientId) {
    if (_manager == null) {
      GD.PushError($"[NetEvent] Attempting to send event '{UniqueId}' to server, but event is not registered");
      return;
    }
    
    _manager.SendClientToServer(this, payload, clientId);
  }

  public void ReceiveMessage(byte[] payload, PeerID senderPeerId, ClientID senderClientId) {
    SERVER_OnMessage?.Invoke(payload.Deserialize<T>(), senderClientId, senderPeerId);
  }
}