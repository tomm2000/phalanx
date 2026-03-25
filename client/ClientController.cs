using System;
using System.Collections.Generic;
using Chickensoft.AutoInject;
using Chickensoft.Introspection;
using Godot;


[Meta(typeof(IAutoConnect), typeof(IAutoNode))]
public abstract partial class ClientController : Node {
  public override void _Notification(int what) => this.Notify(what);

  [Dependency] protected LobbyManager LobbyManager => this.DependOn<LobbyManager>();

  public virtual void OnResolved() {
    Logger.Dev("ClientController resolved.");
    LobbyManager.CurrentGameStage.OnValueChanged += OnGameStageChanged;
  }

  public override void _ExitTree() {
    LobbyManager.CurrentGameStage.OnValueChanged -= OnGameStageChanged;
  }

  protected abstract void OnGameStageChanged(GameStage oldStage, GameStage newStage);
}