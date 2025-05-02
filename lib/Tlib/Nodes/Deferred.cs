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
  private int speed = 1;

  public DeferredQueueExecutor(Node node, int speed = 1) {
    _node = node;
    _node.AddChild(this);
    this.speed = speed;
  }

  public void Add(Action action) {
    _queue.Enqueue(action);
  }

  public override void _Process(double delta) {
    for (int i = 0; i < speed; i++) {
      if (_queue.TryDequeue(out var action)) {
        action.Invoke();
      } else {
        break;
      }
    }
  }
}