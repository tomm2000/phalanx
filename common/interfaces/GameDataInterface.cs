using System;
using System.Collections.Generic;
using Chickensoft.AutoInject;
using Chickensoft.Introspection;
using Godot;

[Meta(typeof(IAutoConnect), typeof(IAutoNode))]
public partial class GameDataInterface : Node {
  public override void _Notification(int what) => this.Notify(what);
  public event Action<Player>? SyncPlayer;

  [Dependency] private GameInstance GameInstance => this.DependOn<GameInstance>();
  [Dependency] private ClientToServerInterface ClientToServerInterface => this.DependOn<ClientToServerInterface>();

  public ServerDictionary<string, PlayerColor> playerColors = default!;
  public ServerValue<MapData?> gameMap = default!;

  public void OnResolved() {
    playerColors = new ServerDictionary<string, PlayerColor>([], this, nameof(playerColors));
    gameMap = new ServerValue<MapData?>(null, this, nameof(gameMap));

    if (!MultiplayerManager.IsHost) { return; }

    GameInstance.SERVER_PlayerReady += (player) => { SyncPlayer?.Invoke(player); };
  }

  [Export] public Godot.Collections.Dictionary<string, PlayerColor> PlayerColors {
    get => playerColors.Value.ToGodotDictionary();
    set { }
  }
}
