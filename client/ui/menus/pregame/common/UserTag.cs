using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Chickensoft.AutoInject;
using Chickensoft.Introspection;
using Godot;
using Steamworks;


[Meta(typeof(IAutoConnect), typeof(IAutoNode))]
public partial class UserTag : Control {
  public override void _Notification(int what) => this.Notify(what);
  public static readonly string ScenePath = "uid://_UID_";

  #region Nodes
  [Node] private TextureRect AvatarTexture { get; set; } = default!;
  [Node] private Label UsernameLabel { get; set; } = default!;
  [Node] private TextureRect SteamIcon { get; set; } = default!;
  #endregion

  public override async void _Ready() {
    if (SteamClient.IsValid && ClientData.SteamId != null) {
      var avatar = await TlibSteam.GetAvatarTextureAsync(ClientData.SteamId.Value, AvatarSize.Medium);
      if (avatar.IsFailed) {
        GD.PrintErr(avatar.Errors);
        return;
      }
      
      AvatarTexture.Texture = avatar.Value;
      AvatarTexture.Visible = true;
      SteamIcon.Visible = true;
    } else {
      SteamIcon.Visible = false;
      MouseDefaultCursorShape = CursorShape.Arrow;
    }

    UsernameLabel.Text = ClientData.Username;
  }

  private void OnGuiInput(InputEvent @event) {
    // if (
    //   @event is InputEventMouseButton mouseButton &&
    //   mouseButton.IsPressed() &&
    //   mouseButton.ButtonIndex == MouseButton.Left &&
    //   Steam.IsSteamRunning()
    // ) {
    //   Steam.ActivateGameOverlay(GameOverlayType.Friends);
    // }
  }
}
