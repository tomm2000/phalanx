using System;
using System.Threading.Tasks;
using Godot;
using Chickensoft.AutoInject;
using Chickensoft.Introspection;
using Tlib.Nodes;
using ImGuiNET;
using System.Collections.Generic;

namespace Client.Terrain;

[Meta(typeof(IAutoConnect))]
public partial class StandardTerrain : Node3D {
  public override void _Notification(int what) => this.Notify(what);
  public static readonly string ScenePath = "uid://bgu2ycrayqu6s";

  [Node] private Node3D TileContainer { get; set; } = default!;
  public IEnumerable<StandardTile> Tiles => TileContainer.GetChildrenOfType<StandardTile>();

  public MapTileData? HoveredTile { get; private set; } = default!;
  public MapTileData? SelectedTile { get; private set; } = default!;

  private TerrainShader activeShader = TerrainShader.Standard;
  public TerrainShader ActiveShader {
    get => activeShader;
    set {
      if (activeShader == value) return;

      activeShader = value;
      
      foreach (var child in TileContainer.GetChildrenOfType<StandardTile>()) {
        child.SetShader(activeShader);
      }
    }
  }

  public override void _Ready() {
    GenerateTerrain();

    ClientEventBus.OnTileLeftClicked += (tile) => { SelectedTile = tile; };
    ClientEventBus.OnTileHovered += (tile) => { HoveredTile = tile; };
  }

  public override void _Process(double delta) {
    DebugUI();
  }

  public void GenerateTerrain() {
    TileContainer.QueueFreeChildren();

    var map = DevMap.GenerateMap(width: 9, height: 9, seed: 3);
    var tiles = new List<StandardTile>();

    foreach (var tile in map.Tiles) {
      var tileInstance = StandardTile.Instantiate(tile);
      TileContainer.AddChild(tileInstance);
      tiles.Add(tileInstance);
    }

    Parallel.ForEach(tiles, tileInstance => {
      var neighbors = map.NeighborsWithDirections(tileInstance.TileData.coords);

      tileInstance.GenerateSurface(neighbors);
    });

    return;
  }

  public Task GenerateTerrain(MapTileData[] tiles) {
    throw new NotImplementedException();
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