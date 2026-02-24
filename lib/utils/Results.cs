using System;
using System.Collections.Generic;
using FluentResults;

public static class ResultsExtensions {
  public static Result<T> FirstOrFailure<T>(this IEnumerable<T> enumerable, Func<T, bool> predicate, string message) {
    foreach (var item in enumerable) {
      if (predicate(item)) {
        return Result.Ok(item);
      }
    }

    return Result.Fail(message);
  }
}