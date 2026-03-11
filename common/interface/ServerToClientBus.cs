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
public partial class ServerToClientBus : Node {
  public override void _Notification(int what) => this.Notify(what);

  #region Remote Values

  #endregion

  #region Nodes
  [Dependency] ClientInterface LobbyManager => this.DependOn<ClientInterface>();
  #endregion

}