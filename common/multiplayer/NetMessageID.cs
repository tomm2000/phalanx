using System;
using Godot;

public readonly record struct NetMessageID {
  public string Value { get; }

  public NetMessageID(string value) {
    if (string.IsNullOrWhiteSpace(value)) {
      throw new ArgumentException("NetMessageID cannot be null, empty, or whitespace.", nameof(value));
    }

    Value = value;
  }

  public static NetMessageID Custom(string value) => new(value);

  public override string ToString() => Value;

  public static explicit operator NetMessageID(string value) => new(value);
  public static implicit operator string(NetMessageID messageID) => messageID.Value;
}

public static class NetMessageIDs {
  public static readonly NetMessageID UnitDeploy = new("UnitDeploy");
  public static readonly NetMessageID UnitDeployed = new("UnitDeployed");
}
