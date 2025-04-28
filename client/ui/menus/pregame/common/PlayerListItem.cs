using System;
using System.Collections.Generic;
using Chickensoft.AutoInject;
using Chickensoft.Introspection;
using Godot;
using GodotSteam;


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

  public override void _Ready() {
    PlayerNameLabel.Text = player.Username;

    if (MultiplayerManager.IsHost) {
      KickButton.Visible = true;
      KickButton.Pressed += OnKickButtonPressed;
    } else {
      KickButton.Visible = false;
    }

    if (Steam.IsSteamRunning() && player.SteamId != null) {
      GD.Print($"Loading avatar for {player.Username} ({player.SteamId})");

      Steam.GetPlayerAvatar(AvatarSize.Medium, player.SteamId.Value);

      Steam.AvatarLoaded += OnAvatarLoaded;
    }
  }

  private void OnKickButtonPressed() {
    MultiplayerManager.SERVER_DisconnectPeer(player.PeerId);
  }

  private void OnAvatarLoaded(ulong avatarId, int width, byte[] data) {
    Steam.AvatarLoaded -= OnAvatarLoaded;

    var avatarImage = Image.CreateFromData(width, width, false, Image.Format.Rgba8, data);

    AvatarTexture.Texture = ImageTexture.CreateFromImage(avatarImage);
  }
}
