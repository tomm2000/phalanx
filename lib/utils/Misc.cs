using System;

public static class MiscUtils {
  public static bool EqualsNullable<T>(T value1, T value2) {
    if (value1 == null && value2 == null) { return true; }
    if (value1 == null || value2 == null) { return false; }

    return value1.Equals(value2);
  }
}

public class OnceCaller {
  private bool hasBeenCalled = false;

  public bool Call(Action action) {
    if (hasBeenCalled) {
      return false;
    }
    action();
    hasBeenCalled = true;
    return true;
  }

  public bool Call<T>(Action<T> action, T arg) {
    if (hasBeenCalled) {
      return false;
    }
    action(arg);
    hasBeenCalled = true;
    return true;
  }
}