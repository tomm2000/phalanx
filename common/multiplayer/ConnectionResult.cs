using MessagePack;
using MessagePack.Resolvers;

public enum ConnectionResultType {
  Success,
  Failure,
}

[MessagePackObject]
public struct ConnectionResult {
  [Key(0)] public ConnectionResultType Result;
  [Key(1)] public string Message;

  public ConnectionResult(ConnectionResultType result, string message) {
    Result = result;
    Message = message;
  }

  public static ConnectionResult Success(string message = "") {
    return new ConnectionResult(ConnectionResultType.Success, message);
  }

  public static ConnectionResult Failure(string message) {
    return new ConnectionResult(ConnectionResultType.Failure, message);
  }

  public byte[] pack() { return MessagePackSerializer.Serialize(this, StandardResolverAllowPrivate.Options); }
  public static ConnectionResult unpack(byte[] data) { return MessagePackSerializer.Deserialize<ConnectionResult>(data, StandardResolverAllowPrivate.Options); }
}