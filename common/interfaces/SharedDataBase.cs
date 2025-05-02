using System;
using System.Collections.Generic;
using Chickensoft.AutoInject;
using Chickensoft.Introspection;
using Godot;
using Steamworks.Data;


[Meta(typeof(IAutoConnect), typeof(IAutoNode))]
public partial class SharedDataBase : Node {
	public override void _Notification(int what) => this.Notify(what);

  [Dependency] GameInstance GameInstance => this.DependOn<GameInstance>();

  public event Action<long>? SyncPeer;

  #region Lobby Properties
  public SharedDictionary<string, bool> LobbyPlayerReadyStatus { get; private set; } = default!;
  public SharedValue<GameStage> CurrentGameStage { get; private set; } = default!;
  public SharedValue<MapData?> SelectedMap { get; private set; } = default!;
  #endregion

  public override void _Ready() {
    LobbyPlayerReadyStatus = new([], this, nameof(LobbyPlayerReadyStatus));
    CurrentGameStage = new(GameStage.Lobby, this, nameof(GameStage));
    SelectedMap = new(null, this, nameof(SelectedMap));
  }


  public void OnResolved() {
    if (!MultiplayerManager.IsHost) return;
    GameInstance.SERVER_PeerInitialized += (peer) => SyncPeer?.Invoke(peer);
  }
}
