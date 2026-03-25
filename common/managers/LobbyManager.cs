using System;
using System.Collections.Generic;
using System.Linq;
using Chickensoft.AutoInject;
using Chickensoft.Introspection;

using Godot;
using Steamworks;
using Tlib.NodeExt;
using Tlib.Serialization;

[Meta(typeof(IAutoConnect), typeof(IAutoNode))]
public partial class LobbyManager : Node {
  public override void _Notification(int what) => this.Notify(what);
  
  #region Nodes
  [Dependency] Main Main => this.DependOn<Main>();
  [Dependency] ClientManager ClientManager => this.DependOn<ClientManager>();
  [Dependency] NetStateManager NetStateManager => this.DependOn<NetStateManager>();
  #endregion

  #region Events
  #endregion

  #region Properties
  public NetDictionary<ClientID, bool> PlayerReadyStatuses { get; init; } = new("PlayerReadyStatuses");
  public NetVar<GameStage> CurrentGameStage { get; init; } = new("CurrentGameStage", GameStage.Disconnected);
  #endregion

  public  void OnResolved() {
    PlayerReadyStatuses.LinkManager(NetStateManager);
    CurrentGameStage.LinkManager(NetStateManager);

    Main.SERVER_NetworkingReady += OnServerNetworkingReady;
  }

  private void OnServerNetworkingReady() {
    CurrentGameStage.SERVER_SetValue(GameStage.Lobby);
  }
}