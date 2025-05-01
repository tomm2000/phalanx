using System;
using System.Collections.Concurrent;
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


public partial class DeferredQueueExecutor: Node {
  private readonly ConcurrentQueue<Action> _queue = new ConcurrentQueue<Action>();
  private readonly Node _node;

  public DeferredQueueExecutor(Node node) {
    _node = node;
    _node.AddChild(this);
  }

  public void Add(Action action) {
    _queue.Enqueue(action);
  }

  public override void _Process(double delta) {
    var action = _queue.TryDequeue(out var result) ? result : null;

    action?.Invoke();
  }
}