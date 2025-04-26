using System;
using System.Collections.Generic;
using Chickensoft.AutoInject;
using Chickensoft.Introspection;
using Godot;
using GodotSteam;


[Meta(typeof(IAutoConnect), typeof(IAutoNode))]
public partial class UserTag : Control {
  public override void _Notification(int what) => this.Notify(what);
  public static readonly string ScenePath = "uid://_UID_";

  #region Nodes
  [Node] private TextureRect AvatarTexture { get; set; } = default!;
  [Node] private Label UsernameLabel { get; set; } = default!;
  [Node] private TextureRect SteamIcon { get; set; } = default!;
  #endregion

  public override void _Ready() {
    if (Steam.IsSteamRunning() && ClientData.SteamId != null) {
      Steam.GetPlayerAvatar(AvatarSize.Medium, ClientData.SteamId.Value);

      Steam.AvatarLoaded += OnAvatarLoaded;
    } else {
      SteamIcon.Visible = false;
      this.MouseDefaultCursorShape = CursorShape.Arrow;
    }

    UsernameLabel.Text = ClientData.Username;
  }

  private void OnAvatarLoaded(ulong avatarId, int width, byte[] data) {
    Steam.AvatarLoaded -= OnAvatarLoaded;

    var avatarImage = Image.CreateFromData(width, width, false, Image.Format.Rgba8, data);
    AvatarTexture.Texture = ImageTexture.CreateFromImage(avatarImage);
  }

  private void OnGuiInput(InputEvent @event) {
    if (
      @event is InputEventMouseButton mouseButton &&
      mouseButton.IsPressed() &&
      mouseButton.ButtonIndex == MouseButton.Left &&
      Steam.IsSteamRunning()
    ) {
      Steam.ActivateGameOverlay(GameOverlayType.Friends);
    }
  }
}
