using Godot;
using System;
using Chickensoft.AutoInject;
using Chickensoft.Introspection;
using ImGuiNET;
using System.IO;
using Steamworks;


using Tlib.NodeExt;
using System.Collections.Generic;

[Meta(typeof(IAutoConnect))]
public partial class Main :
  Node,
  IProvide<Main>,
  IProvide<ClientManager>,
  IProvide<LobbyManager>,
  IProvide<ScenarioManager>,
  IProvide<NetStateManager>,
  IProvide<NetMessageManager>,
  IProvide<SettingsMenu>,
  IProvide<ServerManager>
{
  public static Main Instance { get; set; } = default!;
  public override void _Notification(int what) => this.Notify(what);

  Main IProvide<Main>.Value() => this;
  ClientManager IProvide<ClientManager>.Value() => ClientManager;
  LobbyManager IProvide<LobbyManager>.Value() => LobbyManager;
  ScenarioManager IProvide<ScenarioManager>.Value() => ScenarioManager;
  NetStateManager IProvide<NetStateManager>.Value() => NetStateManager;
  NetMessageManager IProvide<NetMessageManager>.Value() => NetMessageManager;
  ServerManager IProvide<ServerManager>.Value() => ServerManager;
  SettingsMenu IProvide<SettingsMenu>.Value() => SettingsMenu;

  #region Nodes
  [Node] private ClientManager ClientManager { get; set; } = default!;
  [Node] private LobbyManager LobbyManager { get; set; } = default!;
  [Node] private ScenarioManager ScenarioManager { get; set; } = default!;
  [Node] private NetStateManager NetStateManager { get; set; } = default!;
  [Node] private NetMessageManager NetMessageManager { get; set; } = default!;
  [Node] private SettingsMenu SettingsMenu { get; set; } = default!;
  [Node] private ServerManager ServerManager { get; set; } = default!;

  [Node] private Node Clients { get; set; } = default!;
  #endregion

  #region Properties
  private readonly Dictionary<ClientID, ClientInterface> _clientInterfaces = [];
  private ClientInterface MainClientInterface = default!;
  #endregion

  #region Events
  public Action? NetworkingReady;
  public Action? SERVER_NetworkingReady;
  public Action? CLIENT_NetworkingReady;
  
  public Action? NetworkingReset;
  #endregion

  public Main() {
    InitSteam();
  }

  #region Lifecycle
  public override void _Ready() {
    Instance = this;
    this.Provide();

    Client client = GetEmptyClient();
    var clientInterface = SERVER_AttachClient(client);
    clientInterface.AttachController(ClientType.Human);
    MainClientInterface = clientInterface;

    MultiplayerManager.Disconnected += OnDisconnected;
    ClientManager.RegistrationSuccess += OnRegistrationSuccess;
  }

  private Client GetEmptyClient() {
    return new Client(
      uid: "disconnected",
      name: "disconnected",
      peerId: 0,
      connectionStatus: ConnectionStatus.Disconnected,
      joinTime: 0,
      clientType: ClientType.Human,
      steamId: 0
    );
  }

  private void OnRegistrationSuccess(Client client) {
    MainClientInterface.UpdateClient(client);
    NetworkingReady?.Invoke();

    if (MultiplayerManager.IsHost) {
      SERVER_NetworkingReady?.Invoke();
    } else {
      CLIENT_NetworkingReady?.Invoke();
    }
  }

  private void OnDisconnected(MultiplayerDisconnectReason _) {
    MainClientInterface.UpdateClient(GetEmptyClient());
    
    NetworkingReset?.Invoke();
  }

  public override void _Process(double delta) {
    DebugUI();
  }
  #endregion

  #region Steam
  public static void InitSteam() {
    try {
      SteamClient.Init(480, asyncCallbacks: true);
    } catch (System.Exception e) {
      GD.PrintErr($"Steam failed to initialize: {e.Message}");
    }
  }
  
  public static void RestartSteam() {
    try {
      SteamClient.Shutdown();
    } catch (System.Exception e) {
      GD.PrintErr($"Steam failed to shutdown: {e.Message}");
    }

    try {
      SteamClient.Init(480, asyncCallbacks: true);
    } catch (System.Exception e) {
      GD.PrintErr($"Steam failed to initialize: {e.Message}");
    }
  }

  #endregion
  public static void ToggleSettingsMenu(bool forceClose = false) {
    if (forceClose) {
      Instance.SettingsMenu.Visible = false;
      Instance.SettingsMenu.ProcessMode = ProcessModeEnum.Disabled;
      return;
    }
    Instance.SettingsMenu.Visible = !Instance.SettingsMenu.Visible;
    Instance.SettingsMenu.ProcessMode = Instance.SettingsMenu.Visible ? ProcessModeEnum.Always : ProcessModeEnum.Disabled;

  }
  #region Settings Menu

  #endregion

  #region Scene Management
  public ClientInterface SERVER_AttachClient(Client client) {
    Logger.Debug($"Attaching client with UID '{client.UID}' and name '{client.Name}'");

    var clientInterface = ClientInterface.Instantiate(client);

    Clients.AddChild(clientInterface, forceReadableName: true);
    _clientInterfaces[client.UID] = clientInterface;

    return clientInterface;
  }

  public ClientInterface SERVER_DetachClient(Client client) {
    if (_clientInterfaces.TryGetValue(client.UID, out var clientInterface)) {
      clientInterface.QueueFree();
      _clientInterfaces.Remove(client.UID);
      return clientInterface;
    }

    throw new InvalidOperationException($"[Main] No client interface found for client with UID '{client.UID}'!");
  }

  public ClientInterface GetClientInterface(ClientID clientID) {
    if (_clientInterfaces.TryGetValue(clientID, out var clientInterface)) {
      return clientInterface;
    }

    throw new InvalidOperationException($"[Main] No client interface found for client with ID '{clientID}'!");
  }

  public IEnumerable<ClientInterface> GetAllClientInterfaces() => _clientInterfaces.Values;
  #endregion

  #region Debug UI
  private int frameTimeSize = 300;
  private float[] frameTimes = new float[300];
  private int frameTimeIndex = 0;

  private void DebugUI() {
#if DEBUG
    ImGui.Begin("Performance");

    ImGui.Text($"FPS: {Performance.GetMonitor(Performance.Monitor.TimeFps)}");

    var timeProcess = Performance.GetMonitor(Performance.Monitor.TimeProcess);
    ImGui.Text($"Frame time: {Mathf.Round(timeProcess * 1000)}ms");

    frameTimeIndex = (frameTimeIndex + 1) % frameTimeSize;
    frameTimes[frameTimeIndex] = (float)timeProcess * 1000;

    ImGui.PlotLines("Frame time", ref frameTimes[0], frameTimeSize, frameTimeIndex, "ms", 0, 100, new(300, 100));

    var memoryStatic = Performance.GetMonitor(Performance.Monitor.MemoryStatic);
    // display with . separators
    ImGui.Text($"Static memory: {(memoryStatic / 1_000_000).ToString("N0")} MB");

    var objectCount = Performance.GetMonitor(Performance.Monitor.ObjectCount);
    ImGui.Text($"Object count: {objectCount}");

    var orphansCount = Performance.GetMonitor(Performance.Monitor.ObjectOrphanNodeCount);
    ImGui.Text($"Orphan nodes: {orphansCount}");

    var renderObjects = Performance.GetMonitor(Performance.Monitor.RenderTotalObjectsInFrame);
    ImGui.Text($"Render objects: {renderObjects}");

    var renderPrimitives = Performance.GetMonitor(Performance.Monitor.RenderTotalPrimitivesInFrame);
    ImGui.Text($"Render primitives: {renderPrimitives.ToString("N0")}");

    var renderDrawCalls = Performance.GetMonitor(Performance.Monitor.RenderTotalDrawCallsInFrame);
    ImGui.Text($"Render draw calls: {renderDrawCalls}");

    var renderVideoMemory = Performance.GetMonitor(Performance.Monitor.RenderVideoMemUsed);
    ImGui.Text($"Render video memory: {(renderVideoMemory / 1_000_000).ToString("N0")} MB");


    ImGui.End();
#endif
  }
  #endregion
}
