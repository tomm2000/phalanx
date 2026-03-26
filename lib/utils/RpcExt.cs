using System.Collections.Generic;
using System.Linq;
using Godot;
using Tlib.Serialization;

public static class RpcExt {

  public static void TRpc(this Node node, StringName method, params Variant[] args) {
    node.Rpc(method, args);
  }


  public static void TRpcId(this Node node, PeerID peerId, StringName method, params Variant[] args) {
    node.RpcId(peerId, method, args);
  }

  public static void TRpcId(this Node node, IEnumerable<PeerID> peerIds, StringName method, params Variant[] args) {
    foreach (PeerID peerId in peerIds) {
      node.RpcId(peerId, method, args);
    }
  }

  public static void TRpcClient(this Node node, Client client, StringName method, params Variant[] args) {
    node.RpcId(client.PeerId, method, args);
  }

  public static void TRpcClient(this Node node, IEnumerable<Client> clients, StringName method, params Variant[] args) {
    foreach (Client client in clients) {
      node.RpcId(client.PeerId, method, args);
    }
  }
}