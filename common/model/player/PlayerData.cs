using MessagePack;

[MessagePackObject]
public class PlayerData {
  [Key(0)] public PlayerColor Color { get; set; }
}