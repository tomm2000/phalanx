using System;
using System.Collections.Generic;
using Chickensoft.AutoInject;
using Chickensoft.Introspection;
using Godot;
using Steamworks;

namespace Client.UI;

[Meta(typeof(IAutoConnect), typeof(IAutoNode))]
public partial class PlayerListItem : Control {
  public override void _Notification(int what) => this.Notify(what);
  public static readonly string ScenePath = "uid://c68j3jaf4ip0k";

  public static PlayerListItem Instantiate(Player player) {
    var instance = GD.Load<PackedScene>(ScenePath).Instantiate<PlayerListItem>();
    instance.player = player;
    return instance;
  }

  #region Nodes
  [Node] private Label PlayerNameLabel { get; set; } = default!;
  [Node] private Button KickButton { get; set; } = default!;
  [Node] private TextureRect AvatarTexture { get; set; } = default!;
  #endregion

  private Player player;

  public override async void _Ready() {
    PlayerNameLabel.Text = player.Name;

    if (MultiplayerManager.IsHost) {
      KickButton.Visible = true;
      KickButton.Pressed += OnKickButtonPressed;
    } else {
      KickButton.Visible = false;
    }

    if (SteamClient.IsValid && ClientData.SteamId != null && player.SteamId != null) {
      var avatar = await TlibSteam.GetAvatarTextureAsync(player.SteamId!.Value, AvatarSize.Medium);
      
      if (avatar.IsFailed) {
        GD.Print(avatar);
        return;
      }
      AvatarTexture.Texture = avatar.Value;
    }
      
  }

  private void OnKickButtonPressed() {
    MultiplayerManager.KickPeer(player.PeerId);
  }
}
