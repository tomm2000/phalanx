using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using Chickensoft.GodotNodeInterfaces;
using Chickensoft.AutoInject;
using Chickensoft.Introspection;
using RandomNameGeneratorLibrary;
using MessagePack.Resolvers;
using GodotSteam;



public partial class MultiplayerManager : Node {
  /// <summary>
  /// Creates an offline server, used for single player.
  /// </summary>
  public static void HostOffline() {
    var peer = new OfflineMultiplayerPeer();
    Initialize(peer);
  }

  /// <summary>
  /// Hosts a server using the ENet protocol.
  /// </summary>
  public static void HostEnet(int port, int maxConnections = 10) {
    var peer = new ENetMultiplayerPeer();
    var error = peer.CreateServer(port, maxConnections);

    if (error == Error.Ok) {
      Initialize(peer);
    } else {
      GD.PushError($"<enet> Failed to create server: {error}");
    }
  }

  /// <summary>
  /// Connects to a server using the ENet protocol.
  /// </summary>
  public static void ConnectEnet(string address, int port) {
    var peer = new ENetMultiplayerPeer();
    var error = peer.CreateClient(address, port);

    if (error == Error.Ok) {
      Initialize(peer);
    } else {
      GD.PushError($"<enet> Failed to create client: {error}");
    }
  }
  
  /// <summary>
  /// Called by the client to notify the server of a new connection.
  /// </summary>
  [Rpc(
    mode: MultiplayerApi.RpcMode.AnyPeer,
    CallLocal = true,
    TransferMode = MultiplayerPeer.TransferModeEnum.Reliable
  )]
  private void SERVER_EnetClientConnected(string name) {
    if (!IsHost) return;

    var peerId = Multiplayer.GetRemoteSenderId();
    var result = PlayerManager.SERVER_EnetPlayerConnected(name, peerId);
    
    SERVER_ClientConnected(result, peerId);
  }
}