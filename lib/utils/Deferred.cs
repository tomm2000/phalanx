using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using FluentResults;
using Godot;

namespace Tlib;

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
  private int _totalTaskLimit = 0;
  private int _speed = 1;

  public int ProcessedTasks { get; private set; } = 0;
  public int PendingTasks => _queue.Count;
  public int TotalTasks => PendingTasks + ProcessedTasks;

  

  public DeferredQueueExecutor(Node node, int speed = 1, int totalTaskLimit = 0) {
    _node = node;
    _node.AddChild(this);
    _speed = speed;
    _totalTaskLimit = totalTaskLimit;
  }

  public void Add(Action action) {
    if (_totalTaskLimit > 0 && TotalTasks >= _totalTaskLimit) {
      GD.PrintErr($"Total task limit of {_totalTaskLimit} reached. Cannot add more tasks.");
      return;
    }

    _queue.Enqueue(action);
  }

  public override void _Process(double delta) {
    for (int i = 0; i < _speed; i++) {
      if (_queue.TryDequeue(out var action)) {
        action.Invoke();
        ProcessedTasks++;
      } else {
        break;
      }
    }
  }

  public void Clear() {
    while (_queue.TryDequeue(out _)) { }
  }
}