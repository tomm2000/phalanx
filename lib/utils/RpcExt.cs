using System.Collections.Generic;
using System.Linq;
using Godot;
using Tlib.Serialization;

public static class RpcExt {
  private static byte[][] SerializeArgs(object[] args) => [.. args.Select(arg => arg.Serialize())];

  public static void TRpc(this Node node, string method, params object[] args) {
    var serializedArgs = SerializeArgs(args);
    node.Rpc(method, [..serializedArgs]);
  }

  public static void TRpcId(this Node node, PeerID peerId, string method, params object[] args) {
    var serializedArgs = SerializeArgs(args);
    node.RpcId(peerId, method, [..serializedArgs]);
  }

  public static void TRpcId(this Node node, IEnumerable<PeerID> peerIds, string method, params object[] args) {
    var serializedArgs = SerializeArgs(args);

    foreach (PeerID peerId in peerIds) {
      node.RpcId(peerId, method, [..serializedArgs]);
    }
  }

  public static void TRpcClient(this Node node, Client client, string method, params object[] args) {
    var serializedArgs = SerializeArgs(args);
    node.RpcId(client.PeerId, method, [..serializedArgs]);
  }

  public static void TRpcClient(this Node node, IEnumerable<Client> clients, string method, params object[] args) {
    var serializedArgs = SerializeArgs(args);

    foreach (Client client in clients) {
      node.RpcId(client.PeerId, method, [..serializedArgs]);
    }
  }
}