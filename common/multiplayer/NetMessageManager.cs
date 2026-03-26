using System;
using System.Collections.Generic;
using Chickensoft.AutoInject;
using Chickensoft.Introspection;

using Godot;
using Tlib.Serialization;


[Meta(typeof(IAutoConnect), typeof(IAutoNode))]
public partial class NetMessageManager : Node {
  public override void _Notification(int what) => this.Notify(what);

  [Dependency] Main Main => this.DependOn<Main>();

  public delegate void OnClientMessageReceivedHandler<T>(T message, Client client);
  private Action<NetMessageID, byte[], Client>? OnClientMessageReceived;

  public void SERVER_SendMessageToClient<T>(ClientID client, NetMessageID messageID, T payload) {
    if (!MultiplayerManager.IsHost) { throw new InvalidOperationException("Only the host can send messages to clients."); }

    var clientInterface = Main.GetClientInterface(client);

    clientInterface.SERVER_SendMessageToClient(messageID, payload);
  }

  public void SERVER_SendMessageToClient<T>(IEnumerable<ClientID> clients, NetMessageID messageID, T payload) {
    foreach (var client in clients) {
      SERVER_SendMessageToClient(client, messageID, payload);
    }
  }

  public void SERVER_SendMessageToClient<T>(NetMessageID messageID, T payload) {
    foreach (var clientInterface in Main.GetAllClientInterfaces()) {
      clientInterface.SERVER_SendMessageToClient(messageID, payload);
    }
  }

  public void SERVER_RegisterOnClientMessage<T>(NetMessageID messageID, OnClientMessageReceivedHandler<T> handler) {
    OnClientMessageReceived += (receivedMessageID, payload, client) => {
      if (receivedMessageID != messageID) { return; }

      var deserializedPayload = payload.Deserialize<T>();
      handler.Invoke(deserializedPayload, client);
    };
  }

  public void SERVER_InvokeMessageReceived(Client client, NetMessageID messageID, byte[] payload) => OnClientMessageReceived?.Invoke(messageID, payload, client);
}