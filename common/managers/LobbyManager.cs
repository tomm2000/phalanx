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
  [Dependency] NetMessageManager NetMessageManager => this.DependOn<NetMessageManager>();
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

    if (MultiplayerManager.IsHost) {
      Main.SERVER_NetworkingReady += OnServerNetworkingReady;

      NetMessageManager.SERVER_HandleClientMessage<bool>(NetMessageID.RequestLobbyReadyStatusChange, OnRequestLobbyReadyStatusChange);
      NetMessageManager.SERVER_HandleClientMessage<bool>(NetMessageID.RequestStartGame, OnStartGameRequested);
    }
  }

  private void OnServerNetworkingReady() {
    CurrentGameStage.SERVER_SetValue(GameStage.Lobby);
  }

  private void OnRequestLobbyReadyStatusChange(bool newReadyStatus, Client client) {
    PlayerReadyStatuses.SERVER_SetKey(client.UID, newReadyStatus);
  }

  private void OnStartGameRequested(bool _, Client client) {
    // TODO: Only the master can start the game

    CurrentGameStage.SERVER_SetValue(GameStage.Battle);
  }
}