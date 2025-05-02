using System;
using System.Collections.Generic;
using Chickensoft.AutoInject;
using Chickensoft.Introspection;
using Godot;


[Meta(typeof(IAutoConnect), typeof(IAutoNode))]
public partial class PlayerDataInterface : Node {
	public override void _Notification(int what) => this.Notify(what);
	public static readonly string ScenePath = "uid://_UID_";
  
  [Dependency] public ClientInterface ClientInterface => this.DependOn<ClientInterface>();
  public long ClientPeerId => ClientInterface.PeerId;
	
	public static PlayerDataInterface Instantiate() {
    var scene = ResourceLoader.Load<PackedScene>(ScenePath);
    var instance = scene.Instantiate<PlayerDataInterface>();
    return instance;
  }
  
  public event Action? SyncPlayer;

  #region Properties
  #endregion

  public void OnResolved() {

    SyncPlayer?.Invoke();
  }
}
