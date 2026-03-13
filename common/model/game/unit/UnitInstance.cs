
using MessagePack;
using Tlib.Hex;

[MessagePackObject]
public partial class UnitInstance {
  [Key(0)] public UnitInstanceID UID { get; private set; }
  [Key(1)] public DatabaseEntryString BlueprintID { get; private set; }
  [Key(2)] public ClientID OwnerID { get; private set; }
  [Key(3)] public HexCoords Position { get; private set; }

  #region Constructors
  private UnitInstance() { }

  public UnitInstance(
    UnitInstanceID uid,
    DatabaseEntryString blueprintID,
    ClientID ownerID,
    HexCoords position
  ) {
    UID = uid;
    BlueprintID = blueprintID;
    OwnerID = ownerID;
    Position = position;
  }
  #endregion

}