using System;
using System.Collections.Generic;
using Chickensoft.AutoInject;
using Chickensoft.Introspection;
using Godot;

// meta-name: Autonode Template
// meta-description: A template for creating a new autonode script.
// meta-default: true
// meta-space-indent: 2

[Meta(typeof(IAutoConnect), typeof(IAutoNode))]
public partial class _CLASS_ : _BASE_ {
  public override void _Notification(int what) => this.Notify(what);
  public static readonly string ScenePath = "uid://_UID_";

  #region Nodes
  #endregion

  public void OnResolved() {
  }

  public override void _Ready() {
  }
}