using System.Collections.Generic;
using System.Collections.ObjectModel;

public static class TLibDebug {
  public static string PrintList<T>(this List<T> enumerable) {
    var result = "[";
    foreach (var item in enumerable) {
      result += $"{item}, ";
    }
    result += "]";
    return result;
  }
}