using Steamworks;
using Godot;
using System.Threading.Tasks;
using FluentResults;

public enum AvatarSize {
  Small,
  Medium,
  Large
}

public static class TlibSteam {
  public static async Task<Result<ImageTexture>> GetAvatarTextureAsync(SteamId steamId, AvatarSize size) {
    var avatar = size switch {
      AvatarSize.Small => await SteamFriends.GetSmallAvatarAsync(steamId),
      AvatarSize.Medium => await SteamFriends.GetMediumAvatarAsync(steamId),
      AvatarSize.Large => await SteamFriends.GetLargeAvatarAsync(steamId),
      _ => null
    };

    if (avatar == null) { return FluentResults.Result.Fail("Failed to load avatar"); }

    var image = Image.CreateFromData(
      (int)avatar.Value.Width,
      (int)avatar.Value.Height,
      false,
      Image.Format.Rgba8,
      avatar.Value.Data
    );

    var texture = ImageTexture.CreateFromImage(image);
    return FluentResults.Result.Ok(texture);
  }
}