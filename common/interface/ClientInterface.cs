using System;
using System.Collections.Generic;
using Chickensoft.AutoInject;
using Chickensoft.Introspection;

using Godot;


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
  public
  ClientController ClientController { get; set; } = default!;
  #endregion

  #region Properties
  public ClientID ClientID { get; private set; } = default!;

  // FIXME: This is a temporary solution. need to handle multiple players on same peerid.
  public bool IsMaster => MultiplayerManager.IsHost;
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

  public Client GetClient() => ClientManager.GetClient(ClientID);
}
