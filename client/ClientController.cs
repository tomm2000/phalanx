using System;
using System.Collections.Generic;
using Chickensoft.AutoInject;
using Chickensoft.Introspection;
using Godot;


[Meta(typeof(IAutoConnect), typeof(IAutoNode))]
public abstract partial class ClientController : Node {
  public override void _Notification(int what) => this.Notify(what);

  [Dependency] protected PlayerDataInterface ClientInterface => this.DependOn<PlayerDataInterface>();
  [Dependency] protected SharedDataBase SharedDataBase => this.DependOn<SharedDataBase>();

  public virtual void OnResolved() {
    SharedDataBase.CurrentGameStage.ValueChanged += OnGameStageChanged;
  }

  public override void _ExitTree() {
    SharedDataBase.CurrentGameStage.ValueChanged -= OnGameStageChanged;
  }

  protected abstract void OnGameStageChanged(GameStage oldStage, GameStage newStage);
}