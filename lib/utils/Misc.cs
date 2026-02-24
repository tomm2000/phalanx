public static class MiscUtils {
  public static bool EqualsNullable<T>(T value1, T value2) {
    if (value1 == null && value2 == null) { return true; }
    if (value1 == null || value2 == null) { return false; }

    return value1.Equals(value2);
  }
}