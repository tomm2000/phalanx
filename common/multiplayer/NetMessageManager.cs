using System;
using System.Collections.Generic;
using Chickensoft.AutoInject;
using Chickensoft.Introspection;

using Godot;
using Tlib.Serialization;

public enum NetMessageID {
  RequestLobbyReadyStatusChange,
  RequestStartGame,
}


[Meta(typeof(IAutoConnect), typeof(IAutoNode))]
public partial class NetMessageManager : Node {
  public override void _Notification(int what) => this.Notify(what);

  [Dependency] Main Main => this.DependOn<Main>();

  public delegate void OnClientMessageReceivedHandler<T>(T message, Client client);
  private Action<NetMessageID, byte[], Client>? OnClientMessageReceived;

  public void SendMessageToClient<T>(ClientID client, NetMessageID messageID, T payload) {
    if (!MultiplayerManager.IsHost) { throw new InvalidOperationException("Only the host can send messages to clients."); }

    var clientInterface = Main.GetClientInterface(client);

    clientInterface.SERVER_RelayMessageToClient(messageID, payload);
  }

  public void SendMessageToClient<T>(IEnumerable<ClientID> clients, NetMessageID messageID, T payload) {
    foreach (var client in clients) {
      SendMessageToClient(client, messageID, payload);
    }
  }

  public void SendMessageToClient<T>(NetMessageID messageID, T payload) {
    foreach (var clientInterface in Main.GetAllClientInterfaces()) {
      clientInterface.SERVER_RelayMessageToClient(messageID, payload);
    }
  }

  public void SERVER_HandleClientMessage<T>(NetMessageID messageID, OnClientMessageReceivedHandler<T> handler) {
    if (!MultiplayerManager.IsHost) {
      Logger.Warn($"Attempted to register handler for client message {messageID} on non-host. This is not supported and will not work.");
      return;
    }

    OnClientMessageReceived += (receivedMessageID, payload, client) => {
      if (receivedMessageID != messageID) { return; }

      var deserializedPayload = payload.Deserialize<T>();
      handler.Invoke(deserializedPayload, client);
    };
  }

  public void SERVER_InvokeMessageReceived(Client client, NetMessageID messageID, byte[] payload) => OnClientMessageReceived?.Invoke(messageID, payload, client);
}