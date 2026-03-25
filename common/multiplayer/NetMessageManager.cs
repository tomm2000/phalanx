using Godot;
using System.Collections.Generic;
using Tlib.Serialization;

public partial class NetMessageManager : Node {
  private readonly Dictionary<string, List<IServerToClientMessage>> _serversToClientMessages = [];
  private readonly Dictionary<string, List<IClientToServerMessage>> _clientToServerMessages = [];

  public void RegisterMessage(IServerToClientMessage message) {
    if (!_serversToClientMessages.TryGetValue(message.UniqueId, out List<IServerToClientMessage>? value)) {
      value = [];
      _serversToClientMessages[message.UniqueId] = value;
    }

    value.Add(message);
  }

  public void RegisterMessage(IClientToServerMessage message) {
    if (!_clientToServerMessages.TryGetValue(message.UniqueId, out List<IClientToServerMessage>? value)) {
      value = [];
      _clientToServerMessages[message.UniqueId] = value;
    }

    value.Add(message);
  }

  // =========== Server to Client ===========
  public void SendServerToClient<T>(IServerToClientMessage message, T payload, List<Client>? targetClients = null) {
      var serializedPayload = payload.Serialize();

      if (targetClients != null) {
        foreach (var client in targetClients) {
          RpcId(client.PeerId, nameof(RpcServerToClientMessage), message.UniqueId, serializedPayload);
        }
      } else {
        Rpc(nameof(RpcServerToAllMessage), message.UniqueId, serializedPayload);
      }
  }

  [Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
  private void RpcServerToAllMessage(string messageId, byte[] payload) {
    if (_serversToClientMessages.TryGetValue(messageId, out List<IServerToClientMessage>? messages)) {
      foreach (var message in messages) {
        message.ReceiveMessage(payload, 1);
      }
    }
  }

  [Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
  private void RpcServerToClientMessage(string messageId, ClientID clientFilter, byte[] payload) {
    if (_serversToClientMessages.TryGetValue(messageId, out List<IServerToClientMessage>? messages)) {
      foreach (var message in messages) {
        message.ReceiveMessage(payload, 1, clientFilter);
      }
    }
  }
  
  // =========== Client to Server ===========
  public void SendClientToServer<T>(IClientToServerMessage message, T payload, ClientID clientID) {
    var serializedPayload = payload.Serialize();
    
    RpcId(1, nameof(RpcClientToServerMessage), message.UniqueId, clientID, serializedPayload);
  }

  [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
  private void RpcClientToServerMessage(string messageId, ClientID clientId, byte[] payload) {
    PeerID senderPeerId = MultiplayerManager.RpcSenderId();

    // TODO: Validate that the clientId is actually associated with the senderPeerId, to prevent spoofing of other clients' IDs

    if (_clientToServerMessages.TryGetValue(messageId, out List<IClientToServerMessage>? messages)) {
      foreach (var message in messages) {
        message.ReceiveMessage(payload, senderPeerId, clientId);
      }
    }
  }
}