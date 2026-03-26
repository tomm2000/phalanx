using System;
using System.Collections.Generic;
using Chickensoft.AutoInject;
using Chickensoft.Introspection;

using Godot;
using Tlib.Serialization;


[Meta(typeof(IAutoConnect), typeof(IAutoNode))]
public partial class ClientInterface : Node,
  IProvide<ClientInterface>,
  IProvide<ServerToClientBus>,
  IProvide<ClientToServerBus>
{
  public override void _Notification(int what) => this.Notify(what);
  public static readonly string ScenePath = "uid://bs182f4xfpyg5";

  ClientInterface IProvide<ClientInterface>.Value() => this;
  ServerToClientBus IProvide<ServerToClientBus>.Value() => ServerToClientBus;
  ClientToServerBus IProvide<ClientToServerBus>.Value() => ClientToServerBus;

  public static ClientInterface Instantiate(Client client) {
    var scene = ResourceLoader.Load<PackedScene>(ScenePath);
    var instance = scene.Instantiate<ClientInterface>();

    instance.ClientID = client.UID;
    instance.Name = $"ClientInterface_{client.UID}";

    return instance;
  }

  public void AttachController(ClientType clientType) {
    ClientController?.QueueFree();
    ClientController = clientType switch {
      ClientType.Human => PlayerClientController.Instantiate(),
      ClientType.Bot => throw new NotImplementedException("Bot client interface not implemented"),
      _ => throw new NotImplementedException($"Client type {clientType} not implemented"),
    };
    AddChild(ClientController);
  }

  #region Nodes
  [Node] public ServerToClientBus ServerToClientBus { get; private set; } = default!;
  [Node] public ClientToServerBus ClientToServerBus { get; private set; } = default!;
  [Dependency] private ClientManager ClientManager => this.DependOn<ClientManager>();
  [Dependency] private NetMessageManager NetMessageManager => this.DependOn<NetMessageManager>();
  public
  ClientController ClientController { get; set; } = default!;
  #endregion

  #region Properties
  public ClientID ClientID { get; private set; } = default!;

  // FIXME: This is a temporary solution. need to handle multiple players on same peerid.
  public bool IsMaster => MultiplayerManager.IsHost;
  public Client GetClient() => ClientManager.GetClient(ClientID);
  #endregion

  public void UpdateClient(Client client) {
    Logger.Debug($"Updating client interface for client {client.UID} to {client.UID}");
    ClientID = client.UID;
    Name = $"ClientInterface_{client.UID}";
  }

  public override void _Ready() {
    this.Provide();
  }

  public void OnResolved() {
  }
  


  #region Messages
  public delegate void OnServerMessageReceivedHandler<T>(T message);
  private Action<NetMessageID, byte[]>? _onMessageReceived;

  // ======= SERVER TO CLIENT MESSAGES =======
  public void SERVER_SendMessageToClient<T>(NetMessageID messageID, T message) {
    if (!MultiplayerManager.IsHost) { throw new InvalidOperationException("Only the host can send messages to clients."); }

    var client = GetClient();

    this.TRpcClient(client, nameof(RpcReceiveMessage), messageID.Value, message.Serialize());
  }

  [Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
  private void RpcReceiveMessage(string messageID, byte[] message) {
    _onMessageReceived?.Invoke(new NetMessageID(messageID), message);
  }

  public void CLIENT_RegisterOnServerMessage<T>(NetMessageID messageID, OnServerMessageReceivedHandler<T> handler) {
    _onMessageReceived += (receivedMessageID, payload) => {
      if (receivedMessageID != messageID) { return; }

      var deserializedMessage = payload.Deserialize<T>();
      handler.Invoke(deserializedMessage);
    };
  }

  // ======= CLIENT TO SERVER MESSAGES =======
  public void CLIENT_SendMessageToServer<T>(NetMessageID messageID, T message) {
    this.TRpc(nameof(RpcReceiveClientMessage), messageID.Value, message.Serialize());
  }

  [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
  private void RpcReceiveClientMessage(NetMessageID messageID, byte[] message) {
    var client = GetClient();
    var senderId = MultiplayerManager.RpcSenderId();

    if (senderId != client.PeerId) {
      Logger.Warn($"Received message from client with peer ID {senderId}, but expected {client.PeerId}. Ignoring message.");
      return;
    }

    NetMessageManager.SERVER_InvokeMessageReceived(client, messageID, message);
  }
  #endregion

}
