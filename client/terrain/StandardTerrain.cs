using System;
using System.Threading.Tasks;
using Godot;
using Chickensoft.AutoInject;
using Chickensoft.Introspection;
using Tlib.Nodes;
using ImGuiNET;
using System.Collections.Generic;

namespace Client.Terrain;

[Meta(typeof(IAutoConnect), typeof(IAutoNode))]
public partial class StandardTerrain : Node3D, IProvide<StandardTerrain> {
  public override void _Notification(int what) => this.Notify(what);
  public static readonly string ScenePath = "uid://bgu2ycrayqu6s";

  [Dependency] private ClientEventBus ClientEventBus => this.DependOn<ClientEventBus>();

  StandardTerrain IProvide<StandardTerrain>.Value() => this;

  public static StandardTerrain Instantiate() {
    var scene = ResourceLoader.Load<PackedScene>(ScenePath);
    var instance = scene.Instantiate<StandardTerrain>();

    return instance;
  }

  [Node] private Node3D TileContainer { get; set; } = default!;
  private List<ITerrainTile> _tiles { get; set; } = [];
  public IEnumerable<ITerrainTile> Tiles => _tiles;

  public MapTileData? HoveredTile { get; private set; } = default!;
  public MapTileData? SelectedTile { get; private set; } = default!;
  public DeferredQueueExecutor TerrainQueueExecutor { get; private set; } = default!;

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

  public void OnResolved() {
    ClientEventBus.OnTileLeftClicked += (tile) => { SelectedTile = tile; };
    ClientEventBus.OnTileHovered += (tile) => { HoveredTile = tile; };

    TerrainQueueExecutor = new DeferredQueueExecutor(this, 2);

    this.Provide();
  }

  public override void _Process(double delta) {
    DebugUI();
  }

  public Task GenerateTerrain(MapData map) {
    TileContainer.QueueFreeChildren();
    _tiles.Clear();

    foreach (var tile in map.Tiles) {
      var tileInstance = TerrainTile.Instantiate(tile);
      TileContainer.AddChild(tileInstance);
      _tiles.Add(tileInstance);
    }

    var task = Task.Run(() => {
      Parallel.ForEach(_tiles, tileInstance => {
        var neighbors = map.NeighborsWithDirections(tileInstance.TileData.coords);

        tileInstance.GenerateSurface(neighbors);
      });
    });

    // var sw = new System.Diagnostics.Stopwatch();
    // sw.Start();
    // foreach (var tileInstance in _tiles) {
    //   var neighbors = map.NeighborsWithDirections(tileInstance.TileData.coords);
    //   tileInstance.GenerateSurface(neighbors);
    // }
    // sw.Stop();

    // GD.Print($"Terrain generation took {sw.ElapsedMilliseconds}ms");

    return new Task(() => { });
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

    if (HoveredTile != null) {
      ImGui.Text("Hovered Tile: ");
      ImGui.SameLine();
      ImGui.Text(HoveredTile?.ToString() ?? "None");
    } else {
      ImGui.Text("No Tile Hovered");
    }

    if (SelectedTile != null) {
      ImGui.Text("Selected Tile: ");
      ImGui.SameLine();
      ImGui.Text(SelectedTile?.ToString() ?? "None");
    } else {
      ImGui.Text("No Tile Selected");
    }


    ImGui.Separator();
    ImGui.End();
  }
  #endregion
}