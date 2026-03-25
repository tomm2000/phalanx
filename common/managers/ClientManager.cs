using System;
using System.Collections.Generic;
using System.Linq;
using Chickensoft.AutoInject;
using Chickensoft.Introspection;

using FluentResults;
using Godot;
using Steamworks;
using Tlib.Serialization;

[Meta(typeof(IAutoConnect), typeof(IAutoNode))]
public partial class ClientManager : Node {
  public override void _Notification(int what) => this.Notify(what);

  #region Nodes
  [Dependency] Main Main => this.DependOn<Main>();
  #endregion

  #region Lifecycle
  public void OnResolved() {
    MultiplayerManager.SERVER_ClientDisconnected += OnClientDisconnected;

    Logger.Dev("ClientManager resolved, registering client...");

    MultiplayerManager.SERVER_CreatedServer += RegisterClient;
    MultiplayerManager.CLIENT_ConnectedToServer += RegisterClient;

    MultiplayerManager.Disconnected += (_) => {
      clients.Clear();
    };
  }
  #endregion

  #region Client registration
  /// <summary>
  /// Called when a peer (either client or host) is connected to the server.
  /// </summary>
  private void RegisterClient() {
    Logger.Dev("Registering client...");

    if (SteamClient.IsValid) {
      var steamId = SteamClient.SteamId;
      var name = ClientData.Username;
      RpcId(1, nameof(SERVER_RegisterSteamClient), (SteamID) steamId, name);
    } else {
      var name = ClientData.Username;
      RpcId(1, nameof(SERVER_RegisterEnetClient), name);
    }
  }

  [Rpc(
    mode: MultiplayerApi.RpcMode.AnyPeer,
    CallLocal = true,
    TransferMode = MultiplayerPeer.TransferModeEnum.Reliable
  )]
  private void SERVER_RegisterSteamClient(SteamID steamId, string name) {
    if (!MultiplayerManager.IsHost) throw new InvalidOperationException($"[{nameof(SERVER_RegisterSteamClient)}] Only the host can call this method.");

    var peerId = Multiplayer.GetRemoteSenderId();
    var existingClient = Clients.FindBySteamID(steamId);
    Client client;

    if (existingClient.IsFailed) {
      client = new Client(
        uid: Guid.NewGuid().ToString(),
        name: name,
        peerId: peerId,
        steamId: steamId,
        joinTime: DateTime.UtcNow.Ticks,
        clientType: ClientType.Human,
        connectionStatus: ConnectionStatus.Connected
      );
    } else {
      client = existingClient.Value.With(
        peerId: peerId,
        name: name,
        clientType: ClientType.Human
      );
    }

    SERVER_RegisterClient(client, existingClient.IsFailed);
  }

  [Rpc(
    mode: MultiplayerApi.RpcMode.AnyPeer,
    CallLocal = true,
    TransferMode = MultiplayerPeer.TransferModeEnum.Reliable
  )]
  private void SERVER_RegisterEnetClient(string name) {
    if (!MultiplayerManager.IsHost) throw new InvalidOperationException($"[{nameof(SERVER_RegisterEnetClient)}] Only the host can call this method.");

    var peerId = Multiplayer.GetRemoteSenderId();
    var existingClient = Clients.FindByName(name);
    Client client;

    if (existingClient.IsFailed) {
      client = new Client(
        uid: Guid.NewGuid().ToString(),
        name: name,
        peerId: peerId,
        steamId: null,
        joinTime: DateTime.UtcNow.Ticks,
        clientType: ClientType.Human,
        connectionStatus: ConnectionStatus.Connected
      );
    } else {
      client = existingClient.Value.With(
        peerId: peerId,
        name: name,
        steamId: null,
        clientType: ClientType.Human
      );
    }

    SERVER_RegisterClient(client, existingClient.IsSuccess);
  }

  private void SERVER_RegisterClient(Client client, bool existingClient) {
    if (!existingClient) {
      // --------- if the client is not found, create a new one
      if (client.PeerId != MultiplayerManager.PeerId) {
        // Only create a client for remote peers. For local clients it already gets created by registration result.
        Main.Instance.SERVER_AttachClient(client);
      }

      Rpc(nameof(CLIENT_ClientConnected), client.Serialize());

      RpcId(
        client.PeerId,
        nameof(CLIENT_RegistrationResult),
        client.Serialize(),
        true,
        "",
        Clients.ToList().Serialize()
      );

    } else if (client.ConnectionStatus == ConnectionStatus.Disconnected) {
      // --------- if the client exists but is disconnected, reconnect them
      client = client.With(
        connectionStatus: ConnectionStatus.Connected,
        peerId: Multiplayer.GetRemoteSenderId()
      );

      Rpc(nameof(CLIENT_ClientReconnected), client.Serialize());


      RpcId(
        client.PeerId,
        nameof(CLIENT_RegistrationResult),
        client.Serialize(),
        true,
        "",
        Clients.ToList().Serialize()
      );

    } else {
      // --------- if the client exists and is connected, refuse the connection
      RpcId(
        client.PeerId,
        nameof(CLIENT_RegistrationResult),
        new Client().Serialize(),
        false,
        "Client already connected",
        new List<Client>().Serialize()
      );

      Rpc(nameof(CLIENT_ClientFailedToConnect), client.Name, "Client already connected");
    }
  }

  public Action<Client>? RegistrationSuccess;

  [Rpc(
    mode: MultiplayerApi.RpcMode.Authority,
    CallLocal = true,
    TransferMode = MultiplayerPeer.TransferModeEnum.Reliable
  )]
  private void CLIENT_RegistrationResult(
    byte[] clientData,
    bool success,
    string message,
    byte[] clientsData
  ) {
    if (success) {
      var currentClient = clientData.Deserialize<Client?>() ?? throw new InvalidOperationException($"[{nameof(CLIENT_RegistrationResult)}] Client data is null");
      var clientList = clientsData.Deserialize<List<Client>>() ?? throw new InvalidOperationException($"[{nameof(CLIENT_RegistrationResult)}] Clients data is null");

      clients.Clear();
      foreach (var client in clientList) {
        clients.Add(client.UID, client);
      }
      ClientListUpdated?.Invoke();
      RegistrationSuccess?.Invoke(currentClient);

    } else {
      GD.PushError($"Failed to register client: {message}");
    }
  }
  #endregion

  #region Client disconnection
  private void OnClientDisconnected(PeerID peerId) {
    if (!MultiplayerManager.IsHost) throw new InvalidOperationException($"[{nameof(OnClientDisconnected)}] Only the host can call this method.");

    var client = Clients.FindByPeerID(peerId);

    if (client.IsFailed) {
      GD.PushError($"Client not found: {peerId}");
      return;
    }

    var clientValue = client.Value;

    // FIXME: temporary fix for client disconnection
    OnClientQuit(clientValue.UID);

    // if (clientValue.ConnectionStatus == ConnectionStatus.Disconnected) {
    //   GD.PushError($"Client already disconnected: {clientValue.Name}");
    //   return;
    // }

    // clientValue = clientValue.With(
    //   connectionStatus: ConnectionStatus.Disconnected,
    //   peerId: 0
    // );

    // Rpc(nameof(CLIENT_ClientDisconnected), clientValue.Serialize());
  }

  private void OnClientQuit(string clientUID) {
    if (!MultiplayerManager.IsHost) throw new InvalidOperationException($"[{nameof(OnClientQuit)}] Only the host can call this method.");

    var client = Clients.FindByUID(clientUID);

    if (client.IsFailed) {
      GD.PushError($"Client not found: {clientUID}");
      return;
    }

    var clientValue = client.Value;

    Main.Instance.SERVER_DetachClient(clientValue);
    Rpc(nameof(CLIENT_ClientQuit), clientValue.Serialize());

    Logger.Info($"Client quit: {clientValue.Name} ({clientValue.UID})");
  }
  #endregion

  #region Client list
  private Dictionary<ClientID, Client> clients = [];
  public IEnumerable<Client> Clients => clients.Values;

  public event Action? ClientListUpdated;
  public event Action<Client>? ClientConnected;
  public event Action<Client>? ClientReconnected;
  public event Action<string, string>? ClientFailedToConnect;
  public event Action<Client>? ClientDisconnected;
  public event Action<Client>? ClientQuit;

  public Client GetClient(ClientID clientUID) {
    if (clients.TryGetValue(clientUID, out var client)) {
      return client;
    }
    throw new InvalidOperationException($"Client not found: {clientUID}");
  }

  public Result<Client> GetByPeerID(PeerID peerId) {
    return Clients.FindByPeerID(peerId);
  }
  

  [Rpc(mode: MultiplayerApi.RpcMode.Authority, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
  private void CLIENT_ClientConnected(byte[] clientData) {
    var client = clientData.Deserialize<Client?>();

    if (client == null) {
      throw new InvalidOperationException($"[{nameof(CLIENT_ClientConnected)}] Client data is null");
    }

    clients.Remove(client.Value.UID);
    clients.Add(client.Value.UID, client.Value);

    ClientListUpdated?.Invoke();
    ClientConnected?.Invoke(client.Value);
  }

  [Rpc(mode: MultiplayerApi.RpcMode.Authority, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
  private void CLIENT_ClientReconnected(byte[] clientData) {
    var client = clientData.Deserialize<Client?>();

    if (client == null) {
      throw new InvalidOperationException($"[{nameof(CLIENT_ClientReconnected)}] Client data is null");
    }

    clients.Remove(client.Value.UID);
    clients.Add(client.Value.UID, client.Value);

    ClientListUpdated?.Invoke();
    ClientReconnected?.Invoke(client.Value);
  }

  [Rpc(mode: MultiplayerApi.RpcMode.Authority, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
  private void CLIENT_ClientFailedToConnect(string name, string message) {
    ClientFailedToConnect?.Invoke(name, message);
  }

  [Rpc(mode: MultiplayerApi.RpcMode.Authority, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
  private void CLIENT_ClientDisconnected(byte[] clientData) {
    var client = clientData.Deserialize<Client?>() ?? throw new InvalidOperationException($"[{nameof(CLIENT_ClientDisconnected)}] Client data is null");
    clients.Remove(client.UID);
    clients.Add(client.UID, client);

    ClientListUpdated?.Invoke();
    ClientDisconnected?.Invoke(client);
  }

  [Rpc(mode: MultiplayerApi.RpcMode.Authority, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
  private void CLIENT_ClientQuit(byte[] clientData) {
    var client = clientData.Deserialize<Client?>() ?? throw new InvalidOperationException($"[{nameof(CLIENT_ClientQuit)}] Client data is null");
    clients.Remove(client.UID);

    ClientListUpdated?.Invoke();
    ClientQuit?.Invoke(client);
  }

  #endregion
}
