using Godot;
using System.Collections.Generic;
using Tlib.Serialization;

public enum MessageType {
  ClientToServer,
  ServerToClient
}

public partial class NetMessageManager : Node {
  // private readonly Dictionary<string, List<INetMessage>> _listeners = [];
  private readonly Dictionary<string, List<INetMessage>> _serversToClientMessages = [];
  private readonly Dictionary<string, List<INetMessage>> _clientToServerMessages = [];

  public void RegisterMessage(INetMessage message) {
    if (message.MessageType == MessageType.ServerToClient) {
      if (!_serversToClientMessages.TryGetValue(message.UniqueId, out List<INetMessage>? value)) {
        value = [];
        _serversToClientMessages[message.UniqueId] = value;
      }

      value.Add(message);
    } else if (message.MessageType == MessageType.ClientToServer) {
      if (!_clientToServerMessages.TryGetValue(message.UniqueId, out List<INetMessage>? value)) {
        value = [];
        _clientToServerMessages[message.UniqueId] = value;
      }

      value.Add(message);
    }
  }

  // =========== Server to Client ===========
  public void SendServerToClient<T>(INetMessage message, T payload, List<Client>? targetClients = null) {
      if (message.MessageType != MessageType.ServerToClient) {
        GD.PushError($"[NetEventManager] Attempting to send message '{message.UniqueId}' to clients, but it is not marked as ServerToClient.");
        return;
      }

      var serializedPayload = payload.Serialize();

      if (targetClients != null) {
        foreach (var client in targetClients) {
          RpcId(client.PeerId, nameof(RpcServerToClientMessage), message.UniqueId, serializedPayload);
        }
      } else {
        Rpc(nameof(RpcServerToClientMessage), message.UniqueId, serializedPayload);
      }
  }

  [Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
  private void RpcServerToClientMessage(string messageId, ClientID clientFilter, byte[] payload) {
    if (_serversToClientMessages.TryGetValue(messageId, out List<INetMessage>? messages)) {
      foreach (var message in messages) {
        message.ReceiveMessage(payload, clientFilter, 1);
      }
    }
  }
  
  // =========== Client to Server ===========
  public void SendClientToServer<T>(INetMessage message, T payload) {
    if (message.MessageType != MessageType.ClientToServer) {
      GD.PushError($"[NetEventManager] Attempting to trigger event '{message.UniqueId}' to server, but it is not marked as ClientToServer.");
      return;
    }

    var serializedPayload = payload.Serialize();
    
    RpcId(1, nameof(RpcClientToServerMessage), message.UniqueId, message.ClientId, serializedPayload);
  }

  [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
  private void RpcClientToServerMessage(string messageId, ClientID clientId, byte[] payload) {
    PeerID senderPeerId = MultiplayerManager.RpcSenderId();

    // TODO: Validate that the clientId is actually associated with the senderPeerId, to prevent spoofing of other clients' IDs

    if (_clientToServerMessages.TryGetValue(messageId, out List<INetMessage>? messages)) {
      foreach (var message in messages) {
        message.ReceiveMessage(payload, clientId, senderPeerId);
      }
    }
  }
}