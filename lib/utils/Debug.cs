using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

public static class DebugUtils {
  public static string PrintList<T>(this List<T> enumerable) {
    var result = "[";
    foreach (var item in enumerable) {
      result += $"{item}, ";
    }
    result += "]";
    return result;
  }
}

public class TreeFormatter {
  private readonly string _label;
  private readonly List<TreeFormatter> _children = [];

  public TreeFormatter(string label) {
    _label = label;
  }

  public TreeFormatter Add(TreeFormatter child) {
    _children.Add(child);
    return this;
  }

  public TreeFormatter Add(IEnumerable<TreeFormatter> children) {
    _children.AddRange(children);
    return this;
  }

  public string Build() => Render("", "");

  private string Render(string prefix, string childPrefix) {
    var sb = new System.Text.StringBuilder();
    sb.Append(prefix + _label);
    for (int i = 0; i < _children.Count; i++) {
      bool last = i == _children.Count - 1;
      sb.Append('\n');
      sb.Append(_children[i].Render(
        childPrefix + (last ? "└── " : "├── "),
        childPrefix + (last ? "    " : "│   ")
      ));
    }
    return sb.ToString();
  }
}