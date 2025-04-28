using Godot;

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
  

}