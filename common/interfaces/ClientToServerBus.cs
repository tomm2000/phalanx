using System;
using System.Collections.Generic;
using Chickensoft.AutoInject;
using Chickensoft.Introspection;
using Godot;


[Meta(typeof(IAutoConnect), typeof(IAutoNode))]
public partial class ClientToServerBus : Node {
  public override void _Notification(int what) => this.Notify(what);
  [Dependency] public ClientInterface ClientInterface => this.DependOn<ClientInterface>();
  [Dependency] public PlayerDataInterface PlayerDataInterface => this.DependOn<PlayerDataInterface>();
  [Dependency] public SharedDataBase SharedDataBase => this.DependOn<SharedDataBase>();

  #region Common
  private void ValidatePlayer(string where, bool mustBeMaster = false) {
    if (!MultiplayerManager.IsHost) {
      throw new Exception($"[{where}] SERVER methods cannot be called on the host.");
    }

    var expectedPeerId = ClientInterface.PeerId;
    var actualPeerId = MultiplayerManager.RpcSenderId();
    if (expectedPeerId != actualPeerId) {
      throw new Exception($"[{where}] Expected peer ID {expectedPeerId}, but got {actualPeerId}.");
    }

    if (mustBeMaster && !ClientInterface.IsMaster) {
      throw new Exception($"[{where}] Only the master can call this method.");
    }
  }
  #endregion

  #region Lobby
  // ---- Ready Status ----
  public void LobbySetPlayerReady(bool isReady) => RpcId(1, nameof(SERVER_LobbySetPlayerReady), isReady);

  [Rpc(mode: MultiplayerApi.RpcMode.AnyPeer, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
  private void SERVER_LobbySetPlayerReady(bool isReady) {
    ValidatePlayer(nameof(SERVER_LobbySetPlayerReady));

    SharedDataBase.LobbyPlayerReadyStatus.SERVER_SetValue(ClientInterface.PlayerId, isReady);

    SERVER_PlayerSetReady?.Invoke(ClientInterface.PlayerId, isReady);

    GD.Print("Player ", ClientInterface.PlayerId, " is ready: ", isReady);
  }
  public event Action<string, bool>? SERVER_PlayerSetReady;

  // ---- Starting Game ----
  public void LobbyStartGame() => RpcId(1, nameof(SERVER_LobbyStartGame));

  [Rpc(mode: MultiplayerApi.RpcMode.AnyPeer, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
  private void SERVER_LobbyStartGame() {
    ValidatePlayer(nameof(SERVER_LobbyStartGame), mustBeMaster: true);

    SharedDataBase.CurrentGameStage.SERVER_SetValue(GameStage.Battle);

    SERVER_LobbyStartGameEvent?.Invoke();
    SERVER_GameStageChanged?.Invoke(GameStage.Lobby, GameStage.Battle);
  }
  public event Action? SERVER_LobbyStartGameEvent;
  public event Action<GameStage, GameStage>? SERVER_GameStageChanged;

  // ---- Map Selection ----
  public void LobbySelectMap(string mapId) => RpcId(1, nameof(SERVER_LobbySelectMap), mapId);
  [Rpc(mode: MultiplayerApi.RpcMode.AnyPeer, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
  private void SERVER_LobbySelectMap(string mapId) {
    ValidatePlayer(nameof(SERVER_LobbySelectMap), mustBeMaster: true);

    // FIXME: here the map would get loaded from file
    var map = DevMap.GenerateMap(1, 1, seed: 1);

    SharedDataBase.SelectedMap.SERVER_SetValue(map);
    SERVER_MapSelected?.Invoke(mapId);
  }
  public event Action<string>? SERVER_MapSelected;
  #endregion
}
