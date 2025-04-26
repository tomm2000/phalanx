using System;
using System.Collections.Generic;
using System.Linq;

static class TlibRandom {
  /// <summary>
  /// Get a random item from an enumerable
  /// </summary>
  public static T Random<T>(this IEnumerable<T> enumerable) {
    if (!enumerable.Any()) {
      throw new Exception("Cannot get a random item from an empty list");
    }
    var list = new List<T>(enumerable);
    int index = new Random().Next(0, list.Count);
    return list[index];
  }

  public static T RandomRemove<T>(this IList<T> list) {
    if (list.Count == 0) {
      throw new Exception("Cannot get a random item from an empty list");
    }
    int index = new Random().Next(0, list.Count);
    T item = list[index];
    list.RemoveAt(index);
    return item;
  }
}