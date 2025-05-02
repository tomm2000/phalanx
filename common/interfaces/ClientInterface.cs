using System;
using System.Collections.Generic;
using Chickensoft.AutoInject;
using Chickensoft.Introspection;
using Godot;


[Meta(typeof(IAutoConnect), typeof(IAutoNode))]
public partial class ClientInterface : Node,
  IProvide<ClientInterface>,
  IProvide<ClientToServerBus>,
  IProvide<ServerToClientBus>,
  IProvide<PlayerDataInterface>
{
  public override void _Notification(int what) => this.Notify(what);
  public static readonly string ScenePath = "uid://bs182f4xfpyg5";

  ClientInterface IProvide<ClientInterface>.Value() => this;
  ClientToServerBus IProvide<ClientToServerBus>.Value() => ClientToServerBus;
  ServerToClientBus IProvide<ServerToClientBus>.Value() => ServerToClientBus;
  PlayerDataInterface IProvide<PlayerDataInterface>.Value() => PlayerDataInterface;

  public static ClientInterface Instantiate(Player player) {
    var scene = ResourceLoader.Load<PackedScene>(ScenePath);
    var instance = scene.Instantiate<ClientInterface>();
    instance.PlayerId = player.UID;
    instance.Name = $"ClientInterface_{player.UID}";

    return instance;
  }

  public void AttachClientController(ClientController clientController) {
    ClientController?.QueueFree();
    ClientController = clientController;
    AddChild(ClientController);
  }

  #region Nodes
  [Node] ClientToServerBus ClientToServerBus { get; set; } = default!;
  [Node] ServerToClientBus ServerToClientBus { get; set; } = default!;
  [Node] PlayerDataInterface PlayerDataInterface { get; set; } = default!;
  [Dependency] PlayerManager PlayerManager => this.DependOn<PlayerManager>();
  ClientController ClientController { get; set; } = default!;
  #endregion

  #region Properties
  public string PlayerId { get; private set; } = string.Empty;
  public long PeerId => PlayerManager.GetPlayer(PlayerId).PeerId;

  // FIXME: This is a temporary solution. need to handle multiple players on same peerid.
  public bool IsMaster => MultiplayerManager.IsHost;
  #endregion

  public void OnResolved() {
    this.Provide();
  }
}
