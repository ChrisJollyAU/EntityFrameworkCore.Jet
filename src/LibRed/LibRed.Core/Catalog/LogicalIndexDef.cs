using LibRed.Formats;

namespace LibRed.Catalog;

/// <summary>
/// One logical index: a name in the TDEF's logical-index list, pointing at the real index that stores its
/// entries. Several logical indexes share one real index — a foreign key's relationship name, the index the
/// designer named, and a primary key can all sit over the same B-tree — so <see cref="IndexDef"/> (one per
/// real index) cannot represent them all. Access's own schema views list every logical index, which is why
/// the names are kept rather than collapsed to the one that wins. A view over the index-info block it was read
/// from (§3.6).
/// </summary>
public sealed record LogicalIndexDef
{
    internal LogicalIndexDef(TableDefinition.LogicalIndexSpec block) => Block = block;

    /// <summary>The index-info block, every field as read.</summary>
    internal TableDefinition.LogicalIndexSpec Block { get; }

    /// <summary>The logical index's name.</summary>
    public string Name => Block.Name;

    /// <summary>Which real index (<see cref="IndexDef.RealIndexOrdinal"/>) stores it.</summary>
    public int RealIndexOrdinal => Block.DataOrdinal;

    /// <summary>True when the entry is a foreign-key relationship rather than a named index.</summary>
    public bool IsRelationship => Block.FkTablePage != 0;

    /// <summary>True when this entry is the table's primary key.</summary>
    public bool IsPrimaryKey => !IsRelationship && Block.Type == IndexInfoType.Primary;

    /// <summary>The relationship's direction as the info block records it.</summary>
    public ForeignKeyType ForeignKeyType => Block.FkType;

    /// <summary>The info block's update action.</summary>
    public RelationshipAction UpdateAction => Block.UpdateAction;

    /// <summary>The info block's delete action.</summary>
    public RelationshipAction DeleteAction => Block.DeleteAction;

    /// <summary>True for the parent side of a relationship, which Access hides.</summary>
    public bool IsIncomingRelationship => ForeignKeyType == ForeignKeyType.Incoming;
}
