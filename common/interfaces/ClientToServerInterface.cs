using System;
using System.Collections.Generic;
using Chickensoft.AutoInject;
using Chickensoft.Introspection;
using Godot;

[Meta(typeof(IAutoConnect), typeof(IAutoNode))]
public partial class ClientToServerInterface : Node {
  public override void _Notification(int what) => this.Notify(what);
  
  [Dependency] private GameInstance GameInstance => this.DependOn<GameInstance>();
  [Dependency] private GameDataInterface GameDataInterface => this.DependOn<GameDataInterface>();

  public void SelectGameMap(string mapId) {
    RpcId(1, nameof(SERVER_SetGameMap), mapId);
  }

  [Rpc(
    mode: MultiplayerApi.RpcMode.AnyPeer,
    CallLocal = true,
    TransferMode = MultiplayerPeer.TransferModeEnum.Reliable
  )]
  private void SERVER_SetGameMap(string mapId) {
    var sender = PlayerManager.RpcSenderPlayer();

    if (!PlayerManager.PlayerIsMaster(sender.UID)) {
      GD.PrintErr($"[{nameof(ServerToClientInterface)}] {sender.Username} tried to set the game map, but they are not the game master.");
      return;
    }

    // TODO: here the server should load the map from file
    var map = DevMap.GenerateMap(19, 19, seed: 1);
    GameDataInterface.gameMap.SERVER_SetValue(map);

    GD.Print($"[{nameof(ServerToClientInterface)}] {sender.Username} set the game map to {mapId}.");
  }
}
