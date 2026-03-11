using System;
using System.Threading.Tasks;
using Godot;
using Chickensoft.AutoInject;
using Chickensoft.Introspection;
using ImGuiNET;
using System.Collections.Generic;
using Tlib.Hex;
using Tlib.NodeExt;
using Tlib;



[Meta(typeof(IAutoConnect), typeof(IAutoNode))]
public partial class StandardTerrain : Node3D, IProvide<StandardTerrain> {
  public override void _Notification(int what) => this.Notify(what);
  public static readonly string ScenePath = "uid://bgu2ycrayqu6s";

  StandardTerrain IProvide<StandardTerrain>.Value() => this;

  public static StandardTerrain Instantiate() {
    var scene = ResourceLoader.Load<PackedScene>(ScenePath);
    var instance = scene.Instantiate<StandardTerrain>();

    return instance;
  }

  [Node] private Node3D TileContainer { get; set; } = default!;
  private List<ITerrainTile> _tiles { get; set; } = [];
  public IEnumerable<ITerrainTile> Tiles => _tiles;

  public DeferredQueueExecutor MeshApplicationQueue { get; private set; } = default!;

  private TerrainShader activeShader = TerrainShader.Standard;
  public TerrainShader ActiveShader {
    get => activeShader;
    set {
      if (activeShader == value) return;

      activeShader = value;

      foreach (var tile in _tiles) {
        if (tile is ITerrainTile terrainTile) {
          terrainTile.SetShader(activeShader);
        }
      }
    }
  }

  #region Events
  #endregion

  public void OnResolved() {
    this.Provide();

    MeshApplicationQueue = new DeferredQueueExecutor(this, 2);
  }

  public override void _Process(double delta) {
    DebugUI();
  }

  public Task GenerateTerrain(MapData map) {
    TileContainer.QueueFreeChildren();
    MeshApplicationQueue.Clear();
    _tiles.Clear();

    foreach (var tile in map.Tiles) {
      var tileInstance = TerrainTile.Instantiate(tile);
      TileContainer.AddChild(tileInstance);
      _tiles.Add(tileInstance);
    }

    var task = Task.Run(() => {
      Parallel.ForEach(_tiles, tileInstance => {
        var neighbors = map.NeighborsWithDirections(tileInstance.TileData.coords);

        var mesh = tileInstance.GenerateSurface(neighbors);

        MeshApplicationQueue.Add(() => {
          tileInstance.ApplyMesh(mesh);
        });
      });
    });

    return task;
  }

  public HexCoords GetCoords(Vector3 position) {
    throw new NotImplementedException();
  }

  public Vector3 GetPosition(HexCoords coords) {
    throw new NotImplementedException();
  }

  #region Debug UI
  public void DebugUI() {
    ImGui.Begin("Terrain Module");
    ImGui.Separator();

    // build a dropdown for the shader
    ImGui.Text("Shader: ");
    ImGui.SameLine();
    ImGui.SetNextItemWidth(200);
    if (ImGui.BeginCombo("##Shader", ActiveShader.ToString())) {
      foreach (var shader in Enum.GetValues<TerrainShader>()) {
        var isSelected = shader == ActiveShader;
        if (ImGui.Selectable(shader.ToString(), isSelected)) {
          ActiveShader = shader;
        }

        if (isSelected) {
          ImGui.SetItemDefaultFocus();
        }
      }
      ImGui.EndCombo();
    }

    ImGui.Separator();
    ImGui.End();
  }
  #endregion
}