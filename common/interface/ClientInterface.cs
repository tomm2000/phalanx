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
    instance.Client = client;
    instance.Name = $"ClientInterface_{client.UID}";

    return instance;
  }

  public void AttachClientController(Client client) {
    ClientController?.QueueFree();
    ClientController = client.ClientType switch {
      ClientType.Human => PlayerClientController.Instantiate(),
      ClientType.Bot => throw new NotImplementedException("Bot client interface not implemented"),
      _ => throw new NotImplementedException($"Client type {client.ClientType} not implemented"),
    };
    AddChild(ClientController);
  }

  #region Nodes
  [Node] public ServerToClientBus ServerToClientBus { get; private set; } = default!;
  [Node] public ClientToServerBus ClientToServerBus { get; private set; } = default!;
  public
  ClientController ClientController { get; set; } = default!;
  #endregion

  #region Properties
  public Client Client { get; private set; } = default!;

  // FIXME: This is a temporary solution. need to handle multiple players on same peerid.
  public bool IsMaster => MultiplayerManager.IsHost;
  #endregion


  public override void _Ready() {
    this.Provide();
  }

  public void OnResolved() {
  }
}
