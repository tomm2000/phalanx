using System;
using Godot;

namespace Tlib;

/// <summary>
/// A simple struct to calculate a weighted average<br/>
/// <b>Add</b> values with a weight, then get the <b>average</b>
/// </summary>
public struct WeightedAverager {
  float total;
  float weight;
  public float average {
    get {
      if (weight == 0) { return 0; }
      return total / weight;
    }
  }

  public void Add(float value, float weight) {
    this.total += value * weight;
    this.weight += weight;
  }
}

static class Numerics {
  /// <summary>
  /// Returns the distance between a point and a line segment
  /// </summary>
  public static float PointEdgeDistance(Vector2 start, Vector2 end, Vector2 point) {
    var l2 = (end - start).LengthSquared();
    var t = Mathf.Clamp((point - start).Dot(end - start) / l2, 0, 1);
    return (point - (start + t * (end - start))).Length();
  }

  /// <summary>
  /// Returns the distance between a point and a line segment
  /// </summary>
  public static float PointEdgeDistance(Vector4 edge, Vector2 point) {
    return PointEdgeDistance(new Vector2(edge.X, edge.Y), new Vector2(edge.Z, edge.W), point);
  }

  /// <summary>
  /// Applies a logistic smoothing, the function is:<br/>
  /// - [value = -limit] -> 0<br/>
  /// - [value = 0] -> 0.5<br/>
  /// - [value = limit] -> 1<br/>
  /// </summary>
  public static float LogisticSmooth(float value, float limit = 1.0f) {
    var expFactor = -value / 0.2f / limit;
    return 1f / (1f + (float)Math.Exp(expFactor));
  }

  /// <summary>
  /// Returns the value of a number in a new range
  /// </summary>
  public static float SpreadValue(float value, float initialMin, float initialMax, float targetMin, float targetMax) {
    return targetMin + (value - initialMin) * (targetMax - targetMin) / (initialMax - initialMin);
  }

  public static float Spread(this float value, float initialMin, float initialMax, float targetMin, float targetMax) {
    return SpreadValue(value, initialMin, initialMax, targetMin, targetMax);
  }

  /// <summary>
  /// Returns a random number following a Gaussian distribution
  /// </summary>
  public static double GaussianRandom(float mean, float variance, Random? rng = null) {
    // Standard deviation based on the provided variance
    var stdDev = variance;

    rng ??= new Random((int)DateTime.Now.Ticks);

    // Generate a normally distributed value using the Box-Muller transform
    var u1 = 1.0 - rng.NextDouble(); // uniform(0,1] random doubles
    var u2 = 1.0 - rng.NextDouble();
    var randStdNormal = Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Sin(2.0 * Math.PI * u2); // random normal(0,1)
    var randNormal = mean + stdDev * randStdNormal; // random normal(mean, stdDev)

    return randNormal;
  }

  /// <summary>
  /// Wrap a number around a range. Similar to modulo, but always returns a positive number & works with negative numbers
  /// </summary>
  internal static int Wrap(int x, int m) {
    int r = x % m;
    return r < 0 ? r + m : r;
  }
}