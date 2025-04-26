using System.Collections.Generic;
using FluentResults;
using Godot;

namespace Tlib.Nodes;

public static class SceneTreeExpansion {
  /// <summary>
  /// Returns the first parent of the node that is of the specified type
  /// </summary>
  public static Result<T> FindParent<T>(this Node node) where T : Node {
    var parent = node;

    while (parent != null) {
      if (parent is T node1) {
        return node1;
      }
      parent = parent.GetParent();
    }

    return Result.Fail($"Could not find parent of type {typeof(T)}");
  }

  /// <summary>
  /// Returns the first parent of the node that is of the specified type
  /// </summary>
  public static IEnumerable<T> GetChildrenOfType<T>(this Node node) where T : Node {
    foreach (var child in node.GetChildren()) {
      if (child is T childOfType) {
        yield return childOfType;
      }
    }
  }

  /// <summary>
  ///  Returns the first DIRECT child of the node that is of the specified type
  /// </summary>
  public static Result<T> FindChild<T>(this Node node) where T : Node {
    foreach (var child in node.GetChildren()) {
      if (child is T node1) {
        return node1;
      }
    }

    return Result.Fail($"Could not find child of type {typeof(T)}");
  }

  /// <summary>
  /// Returns the first child of the node that is of the specified type, recursively
  /// </summary>
  public static Result<T> FindChildRecursive<T>(this Node node) where T : Node {
    var queue = new Queue<Node>();
    queue.Enqueue(node);

    while (queue.Count > 0) {
      var current = queue.Dequeue();

      foreach (var child in current.GetChildren()) {
        if (child is T) {
          return (T)child;
        }
        queue.Enqueue(child);
      }
    }

    return Result.Fail($"Could not find child of type {typeof(T)}");
  }

  /// <summary>
  /// Frees all children of the node
  /// </summary>
  /// <returns>The number of children that were freed</returns>
  public static int QueueFreeChildren(this Node node) {
    var count = 0;
    foreach (Node child in node.GetChildren()) {
      child.QueueFree();
      count++;
    }

    return count;
  }
}