using System;
using System.Collections.Generic;
using FluentResults;
using Godot;

namespace Tlib.Nodes;

public static class DeferredExtensions {
  /// <summary>
  /// Calls the action on the next frame
  /// </summary>
  public static void CallDeferred(this Node node, Action action) {
    Callable deferred = Callable.From(() => action());

    deferred.CallDeferred();
  }
}