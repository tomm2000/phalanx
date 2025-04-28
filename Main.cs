using Godot;
using System;
using Chickensoft.AutoInject;
using Chickensoft.Introspection;
using ImGuiNET;
using System.IO;
using Steamworks;
using Client.UI.Menus;

[Meta(typeof(IAutoConnect))]
public partial class Main : Node {
  public static Main Instance { get; set; } = default!;
  public override void _Notification(int what) => this.Notify(what);
  
  public Main() {
    InitSteam();
  }

  #region Lifecycle
  public override void _Ready() {
    Instance = this;
    MultiplayerManager.CLIENT_OnConnectionResult += OnClientConnectionResult;
  }

  public override void _Process(double delta) {
    DebugUI();
  }

  public static void Reset() {
    MultiplayerManager.Reset(MultiplayerResetReason.None);
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

  /// <summary>
  /// Called when the user tries to join a lobby from their friends list or from an invite.
  /// </summary>
  private void OnClientConnectionResult(ConnectionResult result) {
    if (result.Result == ConnectionResultType.Success) {
      Main.SwitchScene(MultiplayerLobbyMenu.ScenePath);
    } else {
      GD.PrintErr($"Failed to connect to server: {result.Message}");
    }
  }
  #endregion

  #region Scene Management
  [Export] private Node? CurrentScene { get; set; }

  public static void SwitchScene(string path) {
    SwitchScene((PackedScene) ResourceLoader.Load(path));
  }

  public static void SwitchScene(PackedScene scene) {
    Instance.CurrentScene?.QueueFree();

    Instance.CurrentScene = scene.Instantiate();

    Instance.AddChild(Instance.CurrentScene);
  }

  public static void SwitchScene(Node scene) {
    Instance.CurrentScene?.QueueFree();

    Instance.CurrentScene = scene;

    Instance.AddChild(Instance.CurrentScene);
  }
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
